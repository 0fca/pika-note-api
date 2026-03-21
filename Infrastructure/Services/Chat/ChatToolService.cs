using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenIddict.Client;

namespace PikaNoteAPI.Infrastructure.Services.Chat;

public class ChatToolService : IChatToolService
{
    private readonly IConfiguration _configuration;
    private readonly OpenIddictClientService _openIddictClientService;
    private readonly ILogger<ChatToolService> _logger;

    private List<ChatToolDto>? _cachedTools;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public ChatToolService(
        IConfiguration configuration,
        OpenIddictClientService openIddictClientService,
        ILogger<ChatToolService> logger)
    {
        _configuration = configuration;
        _openIddictClientService = openIddictClientService;
        _logger = logger;
    }

    public async Task<List<ChatToolDto>> GetAvailableToolsAsync()
    {
        if (_cachedTools != null && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedTools;
        }

        try
        {
            var serviceToken = await _openIddictClientService.AuthenticateWithClientCredentialsAsync(
                new OpenIddictClientModels.ClientCredentialsAuthenticationRequest
                {
                    RegistrationId = "base"
                });

            var chatApiUrl = _configuration.GetConnectionString("PikaChatAPI");
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(chatApiUrl!)
            };
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", serviceToken.AccessToken);

            var response = await httpClient.GetAsync("/v1/Tool?visibility=public");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ChatToolService: Failed to fetch tools from PikaChat API, status {StatusCode}", (int)response.StatusCode);
                return _cachedTools ?? [];
            }

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ToolsResponse>(body);
            _cachedTools = result?.Tools?.Select(t => new ChatToolDto
            {
                Uid = t.Uid,
                Name = t.Name,
                Description = t.Description,
                Type = t.Type
            }).ToList() ?? [];
            _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

            _logger.LogInformation("ChatToolService: Cached {Count} tools from PikaChat API", _cachedTools.Count);
            return _cachedTools;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ChatToolService: Exception fetching tools from PikaChat API");
            return _cachedTools ?? [];
        }
    }

    public async Task<bool> IsToolAllowedAsync(string toolName)
    {
        if (string.IsNullOrEmpty(toolName))
        {
            return true;
        }

        var tools = await GetAvailableToolsAsync();
        return tools.Any(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
    }

    private record ToolsResponse
    {
        [JsonPropertyName("tools")]
        public List<ToolItem>? Tools { get; init; }
    }

    private record ToolItem
    {
        [JsonPropertyName("uid")]
        public string Uid { get; init; } = "";
        [JsonPropertyName("name")]
        public string Name { get; init; } = "";
        [JsonPropertyName("description")]
        public string Description { get; init; } = "";
        [JsonPropertyName("type")]
        public string Type { get; init; } = "";
    }
}
