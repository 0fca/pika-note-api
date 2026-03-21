using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PikaNoteAPI.Infrastructure.Services.Chat;

public class ChatToolService : IChatToolService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ChatToolService> _logger;

    private List<ChatToolDto>? _cachedTools;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public ChatToolService(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ChatToolService> logger)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
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
            var identityToken = _httpContextAccessor.HttpContext?.Request.Cookies[".AspNet.Identity"];
            if (string.IsNullOrEmpty(identityToken))
            {
                _logger.LogError("ChatToolService: No .AspNet.Identity cookie present");
                return _cachedTools ?? [];
            }

            var chatApiUrl = _configuration.GetConnectionString("PikaChatAPI");
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(new Uri(chatApiUrl!), new Cookie(".AspNet.Identity", identityToken));
            using var handler = new HttpClientHandler { CookieContainer = cookieContainer };
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(chatApiUrl!)
            };

            var publicTask = httpClient.GetAsync("/v1/Tool?visibility=public");
            var privateTask = httpClient.GetAsync("/v1/Tool?visibility=private");
            await Task.WhenAll(publicTask, privateTask);

            var merged = new List<ChatToolDto>();

            foreach (var response in new[] { publicTask.Result, privateTask.Result })
            {
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("ChatToolService: Failed to fetch tools from PikaChat API, status {StatusCode}", (int)response.StatusCode);
                    continue;
                }

                var body = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ToolsResponse>(body);
                if (result?.Tools != null)
                {
                    merged.AddRange(result.Tools.Select(t => new ChatToolDto
                    {
                        Uid = t.Uid,
                        Name = t.Name,
                        Description = t.Description,
                        Type = t.Type
                    }));
                }
            }

            _cachedTools = merged;
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
