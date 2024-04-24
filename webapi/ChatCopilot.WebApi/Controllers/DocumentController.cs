namespace ChatCopilot.WebApi.Controllers;

[ApiController]
public class DocumentController(
    ILogger<DocumentController> logger,
    IAuthInfo authInfo,
    IContentSafetyService contentSafetyService,
    IOptions<PromptsOptions> promptOptions,
    IOptions<DocumentMemoryOptions> documentMemoryOptions,
    IOptions<ContentSafetyOptions> contentSafetyOptions,
    ChatSessionRepository chatSessionRepository,
    ChatMemorySourceRepository chatMemorySourceRepository,
    ChatMessageRepository chatMessageRepository,
    ChatParticipantRepository chatParticipantRepository,
    DocumentTypeProvider documentTypeProvider) : ControllerBase
{
    private const string GlobalDocumentUploadClientCall = "GlobalDocumentUploaded";
    private const string ReceiveMessageClientCall = "ReceiveMessage";

    private readonly ILogger<DocumentController> _logger = logger;
    private readonly IAuthInfo _authInfo = authInfo;
    private readonly IContentSafetyService _contentSafetyService = contentSafetyService;
    private readonly PromptsOptions _promptOptions = promptOptions.Value;
    private readonly DocumentMemoryOptions _documentMemoryOptions = documentMemoryOptions.Value;
    private readonly ContentSafetyOptions _contentSafetyOptions = contentSafetyOptions.Value;
    private readonly ChatSessionRepository _chatSessionRepository = chatSessionRepository;
    private readonly ChatMemorySourceRepository _chatMemorySourceRepository = chatMemorySourceRepository;
    private readonly ChatMessageRepository _chatMessageRepository = chatMessageRepository;
    private readonly ChatParticipantRepository _chatParticipantRepository = chatParticipantRepository;
    private readonly DocumentTypeProvider _documentTypeProvider = documentTypeProvider;

    [HttpPost]
    [Route("documents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> DocumentImportAsync(
        [FromServices] IKernelMemory kernelMemory,
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        [FromForm] DocumentImportForm documentImportForm)
    {
        return this.DocumentImportAsync(
            kernelMemory,
            messageRelayHubContext,
            DocumentScopes.Global,
            DocumentMemoryOptions.GlobalDocumentChatId,
            documentImportForm);
    }

    [HttpPost]
    [Route("chats/{chatId:guid}/documents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    private Task<IActionResult> DocumentImportAsync(
        [FromServices] IKernelMemory kernelMemory,
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        [FromRoute] Guid chatId,
        [FromForm] DocumentImportForm documentImportForm)
    {
        return this.DocumentImportAsync(
            kernelMemory,
            messageRelayHubContext,
            DocumentScopes.Chat,
            chatId,
            documentImportForm);
    }

    private async Task<IActionResult> DocumentImportAsync(
        IKernelMemory kernelMemory,
        IHubContext<MessageRelayHub> messageRelayHubContext,
        DocumentScopes documentScope,
        Guid chatId,
        DocumentImportForm documentImportForm)
    {
        try
        {
            await this.ValidateDocumentImportFormAsync(chatId, documentScope, documentImportForm);
        }
        catch (ArgumentException ex)
        {
            return this.BadRequest(ex.Message);
        }

        this._logger.LogInformation("Importing {0} document(s)...", documentImportForm.FormFiles.Count());

        DocumentMessageContent documentMessageContent = new();

        List<ImportResult> importResults = await this.ImportDocumentsAsync(kernelMemory, chatId, documentImportForm, documentMessageContent);

        CopilotChatMessage? chatMessage = await this.TryCreateDocumentUploadMessage(chatId, documentMessageContent);

        if (chatMessage == null)
        {
            this._logger.LogWarning("Failed to create document upload message -  {Content}", documentMessageContent.ToString());

            return this.BadRequest();
        }

        if (documentScope == DocumentScopes.Chat)
        {
            string userId = this._authInfo.UserId;

            await messageRelayHubContext.Clients.Group(chatId.ToString())
                .SendAsync(ReceiveMessageClientCall, chatId, userId, chatMessage);

            this._logger.LogInformation("Local upload chat message: {0}", chatMessage.ToString());

            return this.Ok(chatMessage);
        }

        await messageRelayHubContext.Clients.All.SendAsync(
            GlobalDocumentUploadClientCall,
            documentMessageContent.ToFormattedStringNamesOnly(),
            this._authInfo.Name);

        this._logger.LogInformation("Global upload chat message: {0}", chatMessage.ToString());

        return this.Ok(chatMessage);
    }

    private async Task<CopilotChatMessage?> TryCreateDocumentUploadMessage(Guid chatId, DocumentMessageContent messageContent)
    {
        CopilotChatMessage chatMessage = CopilotChatMessage.CreateDocumentMessage(
            this._authInfo.UserId,
            this._authInfo.Name,
            chatId.ToString(),
            messageContent);

        try
        {
            await this._chatMessageRepository.CreateAsync(chatMessage);

            return chatMessage;
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private async Task<List<ImportResult>> ImportDocumentsAsync(IKernelMemory kernelMemory, Guid chatId, DocumentImportForm documentImportForm, DocumentMessageContent messageContent)
    {
        IEnumerable<ImportResult> importResults = [];

        await Task.WhenAll(
            documentImportForm.FormFiles.Select(
                async formFile => await this.ImportDocumentsAsync(formFile, kernelMemory, chatId).ContinueWith(
                    task =>
                    {
                        ImportResult? importResult = task.Result;

                        if (importResult != null)
                        {
                            messageContent.AddDocument(formFile.FileName, this.GetReadableByteString(formFile.Length), importResult.IsSuccessful);

                            importResults = importResults.Append(importResult);
                        }
                    }, TaskScheduler.Default)
                )
            );

        return importResults.ToList();
    }

    private async Task<ImportResult> ImportDocumentsAsync(IFormFile formFile, IKernelMemory kernelMemory, Guid chatId)
    {
        this._logger.LogInformation("Importing document {0}", formFile.FileName);

        MemorySource memorySource = new(chatId.ToString(), formFile.FileName, this._authInfo.UserId, MemorySourceType.File, formFile.Length, hyperlink: null);

        if (!(await this.TryUpsertMemorySourceAsync(memorySource)))
        {
            this._logger.LogDebug("Failed to upsert memory source for file {0}.", formFile.FileName);

            return ImportResult.Fail;
        }

        if (!(await TryStoreMemoryAsync()))
        {
            await this.TryRemoveMemoryAsync(memorySource);
        }

        return new ImportResult(memorySource.Id);

        async Task<bool> TryStoreMemoryAsync()
        {
            try
            {
                using Stream stream = formFile.OpenReadStream();

                await kernelMemory.StoreDocumentAsync(
                    this._promptOptions.MemoryIndexName,
                    memorySource.Id,
                    chatId.ToString(),
                    this._promptOptions.DocumentMemoryName,
                    formFile.FileName,
                    stream);

                return true;
            }
            catch (Exception ex) when (ex is not SystemException)
            {
                return false;
            }
        }
    }

    private async Task<bool> TryUpsertMemorySourceAsync(MemorySource memorySource)
    {
        try
        {
            await this._chatMemorySourceRepository.UpsertAsync(memorySource);

            return true;
        }
        catch (Exception ex) when (ex is not SystemException)
        {
            return false;
        }
    }

    private async Task<bool> TryRemoveMemoryAsync(MemorySource memorySource)
    {
        try
        {
            await this._chatMemorySourceRepository.DeleteAsync(memorySource);

            return true;
        }
        catch (Exception ex) when (ex is not SystemException)
        {
            return false;
        }
    }

    private async Task<bool> UserHasAccessToChatAsync(string userId, Guid chatId)
    {
        return await this._chatParticipantRepository.IsUserInChatAsync(userId, chatId.ToString());
    }

    private async Task ValidateDocumentImportFormAsync(Guid chatId, DocumentScopes scope, DocumentImportForm documentImportForm)
    {
        if (scope == DocumentScopes.Chat
            && !(await this.UserHasAccessToChatAsync(this._authInfo.UserId, chatId)))
        {
            throw new ArgumentException("Use does not have access to the chat session.");
        }

        IEnumerable<IFormFile> formFiles = documentImportForm.FormFiles;

        if (!formFiles.Any())
        {
            throw new ArgumentException("No files were uploaded.");
        }
        else if (formFiles.Count() > this._documentMemoryOptions.FileCountLimit)
        {
            throw new ArgumentException($"Too many files uploaded. Max file count is {this._documentMemoryOptions.FileCountLimit}.");
        }

        foreach (var formFile in formFiles)
        {
            if (formFile.Length == 0)
            {
                throw new ArgumentException($"File {formFile.FileName} is empty.");
            }

            if (formFile.Length > this._documentMemoryOptions.FileSizeLimit)
            {
                throw new ArgumentException($"File {formFile.FileName} size exceeds the limit.");
            }

            string fileType = Path.GetExtension(formFile.FileName);

            if (!this._documentTypeProvider.IsSupported(fileType, out bool isSafetyTarget))
            {
                throw new ArgumentException($"Unsupported file type: {fileType}");
            }

            if (isSafetyTarget && documentImportForm.UseContentSafety)
            {
                if (!this._contentSafetyOptions.Enabled)
                {
                    throw new ArgumentException("Unable to analyze image. Content Safety is currently disabled in the backend.");
                }

                List<string> violations = [];

                try
                {
                    ImageAnalysisResponse imageAnalysisResponse = await this._contentSafetyService.ImageAnalysisAsync(formFile, default);

                    violations = this._contentSafetyService.ParseViolatedCatagories(imageAnalysisResponse, this._contentSafetyOptions.ViolationThreshold);
                }
                catch (Exception ex) when (!ex.IsCriticalException())
                {
                    this._logger.LogError(ex, "Failed to analyze image {0} with Content Safety. Details: {1}", formFile.FileName, ex.Message);

                    throw new AggregateException($"Failed to analyze iamge {formFile.FileName} with Content Safety.", ex);
                }

                if (violations.Count > 0)
                {
                    throw new ArgumentException($"Unable to upload image {formFile.FileName}. Detected undesirable content with potential risk: {string.Join(", ", violations)}");
                }
            }
        }
    }

    private string GetReadableByteString(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];

        int i;

        double dblsBytes = bytes;

        for (i = 0; i < sizes.Length && bytes >= 1024; i++, bytes /= 1024)
        {
            dblsBytes = bytes / 1024;
        }

        return string.Format(CultureInfo.InvariantCulture, "{0:0.#}{1}", dblsBytes, sizes[i]);
    }

    private sealed class ImportResult
    {
        public bool IsSuccessful => !string.IsNullOrWhiteSpace(this.CollectionName);

        public string CollectionName { get; set; }

        public ImportResult(string collectionName)
        {
            this.CollectionName = collectionName;
        }

        public static ImportResult Fail { get; } = new(string.Empty);
    }
}