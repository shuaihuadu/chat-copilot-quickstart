using Microsoft.Identity.Client;
using System.CommandLine;

namespace ImportDocument;

public static class Program
{
    public static void Main(string[] args)
    {
        Config? config = Config.GetConfig();

        if (!Config.Validate(config))
        {
            Console.WriteLine("Error: Faild to read appsettings.json");

            return;
        }

        Option<IEnumerable<FileInfo>> filesOption = new(name: "--files", description: "The files to import to document memory store.")
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true
        };

        Option<Guid> chatCollectionOption = new(name: "--chat-id", description: "Save the extracted context to a isolated chat collection.", getDefaultValue: () => Guid.Empty);

        RootCommand rootCommand = new("This console app imports files to Chat Copilot's WebAPI document memory store.")
        {
            filesOption, chatCollectionOption
        };

        rootCommand.SetHandler(async (files, chatCollectionId) =>
        {
            await ImportFilesAsync(files, config!, chatCollectionId);
        },
        filesOption, chatCollectionOption);

        rootCommand.Invoke(args);
    }

    private static async Task ImportFilesAsync(IEnumerable<FileInfo> files, Config config, Guid chatCollectionId)
    {
        foreach (var file in files)
        {
            if (!file.Exists)
            {
                Console.WriteLine($"File {file.FullName} does not exist.");

                return;
            }
        }

        string? accessToken = null;

        if (config.AuthenticationType == "AzureAd")
        {
            if (await AcquireTokenAsync(config, v => { accessToken = v; }) == false)
            {
                Console.WriteLine("Error: Failed to acquire access token.");

                return;
            }

            Console.WriteLine($"Successfully acquired access token. Continuing...");
        }

        using MultipartFormDataContent formDataContent = new();

        List<StreamContent> filesContent = files.Select(file => new StreamContent(file.OpenRead())).ToList();

        for (int i = 0; i < filesContent.Count; i++)
        {
            formDataContent.Add(filesContent[i], "formFiles", files.ElementAt(i).Name);
        }

        if (chatCollectionId != Guid.Empty)
        {
            Console.WriteLine($"Uploading and parsing file to chat {chatCollectionId}...");

            await UploadAsync(chatCollectionId);
        }
        else
        {
            Console.WriteLine("Uploading and parsing file to global collection...");

            await UploadAsync();
        }

        foreach (var fileContent in filesContent)
        {
            fileContent.Dispose();
        }

        async Task UploadAsync(Guid? chatId = null)
        {
            using HttpClientHandler clientHandler = new()
            {
                CheckCertificateRevocationList = true
            };

            using HttpClient httpClient = new(clientHandler)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };

            if (config.AuthenticationType == "AzureAd")
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            }

            string uriPath = chatId.HasValue ? $"chats/{chatId}/documents" : "documents";

            try
            {
                using HttpResponseMessage response = await httpClient.PostAsync(
                    new Uri(new Uri(config.ServiceUri), uriPath),
                    formDataContent);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error: {response.StatusCode} {response.ReasonPhrase}");
                    Console.WriteLine(await response.Content.ReadAsStringAsync());

                    return;
                }

                Console.WriteLine("Uploading and parsing successful.");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }


    private static async Task<bool> AcquireTokenAsync(Config config, Action<string> setAccessToken)
    {
        Console.WriteLine("Attempting to authenticate user...");

        string webApiScope = $"api://{config.BackendClientId}/{config.Scopes}";

        string[] scopes = { webApiScope };

        try
        {
            IPublicClientApplication app = PublicClientApplicationBuilder.Create(config.ClientId)
                .WithRedirectUri(config.RedirectUri)
                .WithAuthority(config.Instance, config.TenantId)
                .Build();

            AuthenticationResult result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();

            setAccessToken(result.AccessToken);

            return true;
        }
        catch (Exception ex) when (ex is MsalServiceException or MsalClientException)
        {
            Console.WriteLine($"Error: {ex.Message}");

            return false;
        }
    }
}
