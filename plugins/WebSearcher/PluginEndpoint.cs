using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using Plugins.PluginShared;
using Plugins.WebSearcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Plugins.WebSearcher;

public class PluginEndpoint
{
    private readonly ILogger<PluginEndpoint> _logger;
    private readonly BingConfig _bingConfig;

    public PluginEndpoint(ILogger<PluginEndpoint> logger, BingConfig bingConfig)
    {
        this._logger = logger;
        this._bingConfig = bingConfig;
    }

    [Function("WellKnownAIPluginManifest")]
    public async Task<HttpResponseData> WellKnownAIPluginManifest([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ".well-known/ai-plugin.json")] HttpRequestData request)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        PluginManifest pluginManifest = new()
        {
            NameForModel = "WebSearcher",
            NameForHuman = "WebSearcher",
            DescriptionForModel = "Searches the web",
            DescriptionForHuman = "Searches the web",
            Auth = new PluginAuth
            {
                Type = "user_http"
            },
            Api = new PluginApi
            {
                Type = "openapi",
                Url = $"{request.Url.Scheme}://{request.Url.Host}:{request.Url.Port}/swagger.json"
            },
            LogoUrl = $"{request.Url.Scheme}://{request.Url.Host}:{request.Url.Port}/.well-know/icon"
        };

        HttpResponseData? response = request.CreateResponse(HttpStatusCode.OK);

        await response.WriteAsJsonAsync(pluginManifest);

        return response;
    }

    [Function("Icon")]
    public async Task<HttpResponseData> Icon([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ".well-known/icon")] HttpRequestData request)
    {
        if (!File.Exists("./Icon/bing.png"))
        {
            return request.CreateResponse(HttpStatusCode.NotFound);
        }

        using Stream stream = new FileStream("./Icon/bing.png", FileMode.Open);

        HttpResponseData response = request.CreateResponse(HttpStatusCode.OK);

        response.Headers.Add("Content-Type", "image/png");

        await stream.CopyToAsync(response.Body);

        return response;
    }

    [OpenApiOperation(operationId: "Search", tags: new[] { "WebSearchFunction" }, Description = "Searches the web for the given query.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
    [OpenApiParameter(name: "Query", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The query")]
    [OpenApiParameter(name: "NumResults", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "The maximum number of results to return")]
    [OpenApiParameter(name: "Offset", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "The number of results to skip")]
    [OpenApiParameter(name: "Site", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "The specific site to search within")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "Returns a collection of search results with the name, URL and snippet for each.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Invalid query")]
    [Function("WebSearch")]
    public async Task<HttpResponseData> WebSearch([HttpTrigger(AuthorizationLevel.Function, "get", Route = "search")] HttpRequestData request)
    {
        Dictionary<string, StringValues> queries = QueryHelpers.ParseQuery(request.Url.Query);

        string query = queries.ContainsKey("Query") ? queries["Query"].ToString() : string.Empty;

        if (string.IsNullOrWhiteSpace(query))
        {
            return await this.CreateBadRequestResponseAsync(request, "Empty query.");
        }

        int numResults = queries.ContainsKey("NumResults") ? int.Parse(queries["NumResults"]) : 0;

        if (numResults <= 0)
        {
            return await this.CreateBadRequestResponseAsync(request, "Invalid number of results.");
        }

        int offset = 0;

        if (queries.TryGetValue("Offset", out StringValues offsetValue))
        {
            int.TryParse(offsetValue, out offset);
        }

        string site = queries.ContainsKey("Site") ? queries["Site"].ToString() : string.Empty;

        if (string.IsNullOrWhiteSpace(site))
        {
            this._logger.LogDebug("Searching the web for '{0}'", query);
        }
        else
        {
            this._logger.LogDebug("Searching the web for '{0}' within '{1}'", query, site);
        }

        using HttpClient httpClient = new HttpClient();

        string queryString = $"?q={Uri.EscapeDataString(query)}";
        queryString += string.IsNullOrWhiteSpace(site) ? string.Empty : $"+site:{site}";
        queryString += $"&count={numResults}";
        queryString += $"&offset={offset}";

        Uri uri = new($"{this._bingConfig.BingApiBaseUrl}{queryString}");

        this._logger.LogDebug("Sending request to {0}", uri);

        httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", this._bingConfig.ApiKey);

        string bingResponse = await httpClient.GetStringAsync(uri);

        this._logger.LogDebug("Search completed. Response: {0}", bingResponse);

        BingSearchResponse? bingSearchResponse = JsonSerializer.Deserialize<BingSearchResponse>(bingResponse);

        WebPage[]? results = bingSearchResponse?.WebPages?.Value;

        string responseText = results == null
            ? "No results found."
            : string.Join(",", results.Select(r => $"[NAME]{r.Name}[END NAME] [URL]{r.Url}[END URL] [SNIPPET]{r.Snippet}[END SNIPPET]"));

        return await this.CreateOkResponseAsync(request, responseText);
    }

    private async Task<HttpResponseData> CreateOkResponseAsync(HttpRequestData request, string content)
    {
        HttpResponseData response = request.CreateResponse(HttpStatusCode.OK);

        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        await response.WriteStringAsync(content);

        return response;
    }

    private async Task<HttpResponseData> CreateBadRequestResponseAsync(HttpRequestData request, string errorMessage)
    {
        this._logger.LogError(errorMessage);

        HttpResponseData response = request.CreateResponse(HttpStatusCode.BadRequest);

        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        await response.WriteStringAsync(errorMessage);

        return response;
    }
}
