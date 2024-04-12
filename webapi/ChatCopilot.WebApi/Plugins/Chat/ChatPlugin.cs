
using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatCopilot.WebApi.Plugins.Chat
{
    internal class ChatPlugin
    {
        private readonly Kernel _kernel;
        private readonly IKernelMemory _kernelMemory;
        private readonly IHubContext<MessageRelayHub> _messageRelayHubContext;
        private readonly ILogger<ChatPlugin> _logger;
        private readonly PromptsOptions _promptOptions;
        private readonly DocumentMemoryOptions _documentImportOptions;
        private readonly AzureContentSafty _contentSafety;
        private readonly ChatMessageRepository _chatMessageRepository;
        private readonly ChatSessionRepository _chatSessionRepository;

        private readonly SemanticMemoryRetriever _semanticMemoryRetriever;


        public ChatPlugin(
            Kernel kernel,
            IKernelMemory kernelMemory,
            IHubContext<MessageRelayHub> messageRelayHubContext,
            ILogger<ChatPlugin> logger,
            IOptions<PromptsOptions> promptOptions,
            IOptions<DocumentMemoryOptions> documentImportOptions,
            ChatMessageRepository chatMessageRepository,
            ChatSessionRepository chatSessionRepository,
            AzureContentSafty contentSafety)
        {
            this._kernel = kernel;
            this._kernelMemory = kernelMemory;
            this._messageRelayHubContext = messageRelayHubContext;
            this._logger = logger;
            this._promptOptions = promptOptions.Value;
            this._documentImportOptions = documentImportOptions.Value;
            this._chatMessageRepository = chatMessageRepository;
            this._chatSessionRepository = chatSessionRepository;
            this._contentSafety = contentSafety;

            this._semanticMemoryRetriever = new SemanticMemoryRetriever(promptOptions, kernelMemory, logger, chatSessionRepository);
        }

        [KernelFunction, Description("Extract chat history")]
        public Task<string> ExtractChatHistory(
            [Description("Chat ID to extract history from")] string chatId,
            [Description("Maximum number of tokens")] int tokenLimit,
            CancellationToken cancellationToken = default)
        {
            return this.GetAllowedChatHistoryAsync(chatId, tokenLimit, cancellationToken: cancellationToken);
        }

        [KernelFunction, Description("Get chat response")]
        public async Task<KernelArguments> ChatAsync(
            [Description("The new message")] string message,
            [Description("Unique and persistent identifier for the user")] string userId,
            [Description("Name of the user")] string userName,
            [Description("Unique and persistent identifier for the chat")] string chatId,
            [Description("Type of the message")] string messageType,
            KernelArguments context,
            CancellationToken cancellationToken = default)
        {
            await this.SetSystemDescriptionAsync(chatId, cancellationToken);

            await this.UpdateBotResponseStatusOnClientAsync(chatId, "Saving user message to chat history", cancellationToken);

            CopilotChatMessage newUserMessage = await this.SaveNewMessageAsync(message, userId, userName, chatId, messageType, cancellationToken);

            KernelArguments chatContext = new(context);
            chatContext["knowledgeCutoff"] = this._promptOptions.KnowledgeCutoffDate;

            CopilotChatMessage chatMessage = await this.GetChatResponseAsync(chatId, userId, chatContext, newUserMessage, cancellationToken);
            context["input"] = chatMessage.Content;

            if (chatMessage.TokenUsage != null)
            {
                context["tokenUsage"] = JsonSerializer.Serialize(chatMessage.TokenUsage);
            }
            else
            {
                this._logger.LogWarning("ChatPlugin.ChatAsync token usage unknown. Ensure token management has been implemented correctly.");
            }

            return context;
        }

        private async Task<string> GetAllowedChatHistoryAsync(
            string chatId,
            int tokenLimit,
            ChatHistory? chatHistory = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<CopilotChatMessage> sortedMessages = await this._chatMessageRepository.FindByChatIdAsync(chatId, 0, 100);

            ChatHistory allottedChatHistory = [];

            int remainningToken = tokenLimit;

            string historyText = string.Empty;

            foreach (var chatMessage in sortedMessages)
            {
                string formattedMessage = chatMessage.ToFormattedString();

                if (chatMessage.Type == CopilotChatMessage.ChatMessageType.Document)
                {
                    continue;
                }

                AuthorRole promptRole = chatMessage.AuthorRole == CopilotChatMessage.AuthorRoles.Bot ? AuthorRole.System : AuthorRole.User;

                int tokenCount = chatHistory is not null ? TokenUtils.GetContextMessageTokenCount(promptRole, formattedMessage) : TokenUtils.TokenCount(formattedMessage);

                if (remainningToken - tokenCount >= 0)
                {
                    historyText = $"{formattedMessage}\n{historyText}";

                    if (chatMessage.AuthorRole == CopilotChatMessage.AuthorRoles.Bot)
                    {
                        allottedChatHistory.AddAssistantMessage(chatMessage.Content.Trim());
                    }
                    else
                    {
                        string? userMessage = PassThroughAuthenticationHandler.IsDefaultUser(chatMessage.UserId)
                            ? $"[{chatMessage.Timestamp.ToString("G", CultureInfo.CurrentCulture)}] {chatMessage.Content}"
                            : formattedMessage;
                    }

                    remainningToken -= tokenCount;
                }
                else
                {
                    break;
                }
            }

            chatHistory?.AddRange(allottedChatHistory.Reverse());

            return $"Chat history:\n{historyText.Trim()}";
        }

        private async Task<CopilotChatMessage> GetChatResponseAsync(string chatId, string userId, KernelArguments chatContext, CopilotChatMessage userMessage, CancellationToken cancellationToken)
        {
            //设置System Instruction
            string? systemInstructions = await AsyncUtils.SafeInvokeAsync(() => this.RenderSystemInstructions(chatId, chatContext, cancellationToken), nameof(RenderSystemInstructions));

            ChatHistory metaPrompt = new(systemInstructions);

            string audience = string.Empty;

            if (!PassThroughAuthenticationHandler.IsDefaultUser(userId))
            {
                await this.UpdateBotResponseStatusOnClientAsync(chatId, "Extracting audience", cancellationToken);
                audience = await AsyncUtils.SafeInvokeAsync(() => this.GetAudienceAsync(chatContext, cancellationToken), nameof(GetAudienceAsync));
                metaPrompt.AddSystemMessage(audience);
            }

            //获取用户意图
            await this.UpdateBotResponseStatusOnClientAsync(chatId, "Extracting user intent", cancellationToken);
            string userIntent = await AsyncUtils.SafeInvokeAsync(() => this.GetUserIntentAsync(chatContext, cancellationToken), nameof(GetUserIntentAsync));
            metaPrompt.AddSystemMessage(userIntent);

            //计算Memory Query的Token Limit
            int maxRequestTokenBudget = this.GetMaxRequestTokenBudget();
            int tokensUsed = TokenUtils.GetContextMessageTokenCount(metaPrompt);
            int chatMemoryTokenBudget = maxRequestTokenBudget
                - tokensUsed
                - TokenUtils.GetContextMessageTokenCount(AuthorRole.User, userMessage.ToFormattedString());

            chatMemoryTokenBudget = (int)(chatMemoryTokenBudget * this._promptOptions.MemoriesResponseContextWeight);

            await this.UpdateBotResponseStatusOnClientAsync(chatId, "Extracting semantic and document memories", cancellationToken);
            (string memoryText, IDictionary<string, CitationSource>? citationMap) = await this._semanticMemoryRetriever.QueryMemoriesAsync(userIntent, chatId, chatMemoryTokenBudget);

            if (!string.IsNullOrWhiteSpace(memoryText))
            {
                metaPrompt.AddSystemMessage(memoryText);
                tokensUsed += TokenUtils.GetContextMessageTokenCount(AuthorRole.System, memoryText);
            }

            await this.UpdateBotResponseStatusOnClientAsync(chatId, "Extract chat history", cancellationToken);
            string allowedChatHisotry = await this.GetAllowedChatHistoryAsync(chatId, maxRequestTokenBudget - tokensUsed, metaPrompt, cancellationToken);

            chatContext[TokenUtils.GetFunctionKey("SystemMetaPrompt")] = TokenUtils.GetContextMessageTokenCount(metaPrompt).ToString(CultureInfo.CurrentCulture);

            BotResponsePrompt promptView = new BotResponsePrompt(systemInstructions, audience, userIntent, memoryText, allowedChatHisotry, metaPrompt);

            return await this.HandleBotResponseAsync(chatId, userId, chatContext, promptView, citationMap.Values.AsEnumerable(), cancellationToken);
        }

        private async Task<CopilotChatMessage> SaveNewMessageAsync(string message, string userId, string userName, string chatId, string messageType, CancellationToken cancellationToken)
        {
            if (!await this._chatSessionRepository.TryFindByIdAsync(chatId))
            {
                throw new ArgumentException("Chat session does not exists.");
            }

            CopilotChatMessage chatMessage = new CopilotChatMessage(
                userId,
                userName,
                chatId,
                message,
                string.Empty,
                null,
                CopilotChatMessage.AuthorRoles.User,
                Enum.TryParse(messageType, out CopilotChatMessage.ChatMessageType typeAsEnum) && Enum.IsDefined(typeof(CopilotChatMessage.ChatMessageType), typeAsEnum)
                    ? typeAsEnum
                    : CopilotChatMessage.ChatMessageType.Message);

            await this._chatMessageRepository.CreateAsync(chatMessage);

            return chatMessage;
        }

        private async Task UpdateBotResponseStatusOnClientAsync(string chatId, string status, CancellationToken cancellationToken)
        {
            await this._messageRelayHubContext.Clients.Group(chatId).SendAsync("ReceiveBotResponseStatus", chatId, status, cancellationToken);
        }

        private async Task SetSystemDescriptionAsync(string chatId, CancellationToken cancellationToken)
        {
            ChatSession? chatSession = null;

            if (!await this._chatSessionRepository.TryFindByIdAsync(chatId, callback: v => chatSession = v))
            {
                throw new ArgumentException("Chat session does not exists.");
            }

            this._promptOptions.SystemDescription = chatSession!.SafeSystemDescription;
        }
    }
}