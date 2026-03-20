using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PikaNoteAPI.Infrastructure.Services.Security;

public class SecurityService : ISecurityService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly CookieContainer _cookieContainer;
    private readonly ILogger<SecurityService> _logger;
    
    public SecurityService(
        IConfiguration configuration,
        ILogger<SecurityService> logger
        )
    {
        this._configuration = configuration;
        this._logger = logger;
        this._cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler() { CookieContainer = this._cookieContainer };
        this._http = new HttpClient(handler)
        {
            BaseAddress = new Uri(_configuration.GetConnectionString("PikaCoreAPI")) 
        };
    }

    public async Task ConfigureAccessToken(string token, IEnumerable<Claim> claims)
    {
        
        this._cookieContainer.Add(this._http.BaseAddress, new Cookie(".AspNet.Identity", token));
    }
    
    public async Task<Dictionary<Guid, bool>?> HasNotesAccess(string token, Dictionary<Guid, Guid> bids)
    {
        var content = JsonSerializer.Serialize(new
        { 
            token,
            bids
        });
        var response = await this._http.SendAsync(new HttpRequestMessage
        {
            RequestUri = new Uri(_http.BaseAddress?.AbsoluteUri+"/NoteStorage/Security/ABAN"),
            Method = HttpMethod.Get,
            Content = new StringContent(content, Encoding.UTF8, MediaTypeNames.Application.Json)
        });
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = JsonSerializer.Deserialize<Dictionary<Guid, bool>>(await response.Content.ReadAsStringAsync());
        return result;
    }
    
    public async Task<TokenValidationResult> CheckTokenValidityAsync(string? identityCookie)
    {
        if (string.IsNullOrEmpty(identityCookie))
        {
            return TokenValidationResult.Failure();
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(identityCookie);
            var jwst = jsonToken as JwtSecurityToken;
            if (jwst == null)
            {
                _logger.LogWarning("CheckTokenValidity: failed to parse JWT from identity cookie");
                return TokenValidationResult.Failure();
            }
            if (jwst.ValidTo <= DateTime.UtcNow)
            {
                _logger.LogWarning("CheckTokenValidity: access token expired at {ValidTo}", jwst.ValidTo);
                return TokenValidationResult.Failure();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CheckTokenValidity: exception while parsing JWT");
            return TokenValidationResult.Failure();
        }

        try
        {
            var cookieContainer = new CookieContainer();
            var httpHandler = new HttpClientHandler { CookieContainer = cookieContainer };
            using var httpClient = new HttpClient(httpHandler)
            {
                BaseAddress = new Uri(_configuration.GetConnectionString("PikaCore"))
            };
            cookieContainer.Add(httpClient.BaseAddress!, new Cookie(".AspNet.Identity", identityCookie));

            _logger.LogWarning("CheckTokenValidity: calling PikaCore Status endpoint at {BaseAddress}/Identity/Gateway/Status", httpClient.BaseAddress);
            var response = await httpClient.GetAsync("/Identity/Gateway/Status");
            _logger.LogWarning("CheckTokenValidity: call returned {StatusCode}", (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("CheckTokenValidity: PikaCore Status returned non-success status {StatusCode}", (int)response.StatusCode);
                return TokenValidationResult.Failure();
            }

            var body = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("CheckTokenValidity: PikaCore Status response body: {Body}", body);
            var doc = JsonDocument.Parse(body);
            var isAuthenticated = doc.RootElement.TryGetProperty("isAuthenticated", out var authEl)
                                  && (authEl.ValueKind == JsonValueKind.True || authEl.ValueKind == JsonValueKind.False)
                                  && authEl.GetBoolean();
            var hasUsername = doc.RootElement.TryGetProperty("username", out var userEl)
                             && userEl.ValueKind == JsonValueKind.String
                             && !string.IsNullOrEmpty(userEl.GetString());
            if (!isAuthenticated || !hasUsername)
            {
                _logger.LogWarning("CheckTokenValidity: validation failed - isAuthenticated={IsAuthenticated}, hasUsername={HasUsername}", isAuthenticated, hasUsername);
            }
            return (isAuthenticated && hasUsername) ? TokenValidationResult.Success() : TokenValidationResult.Failure();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CheckTokenValidity: exception during PikaCore Status call");
            return TokenValidationResult.Failure();
        }
    }

    public async Task<TokenValidationResult> RefreshTokenAsync(string? identityCookie, string? refreshCookie)
    {
        if (string.IsNullOrEmpty(refreshCookie))
        {
            _logger.LogWarning("RefreshToken: no refresh cookie present");
            return TokenValidationResult.Failure();
        }

        try
        {
            var cookieContainer = new CookieContainer();
            var httpHandler = new HttpClientHandler { CookieContainer = cookieContainer };
            using var httpClient = new HttpClient(httpHandler)
            {
                BaseAddress = new Uri(_configuration.GetConnectionString("PikaCore"))
            };
            if (!string.IsNullOrEmpty(identityCookie))
            {
                cookieContainer.Add(httpClient.BaseAddress!, new Cookie(".AspNet.Identity", identityCookie));
            }
            cookieContainer.Add(httpClient.BaseAddress!, new Cookie(".AspNet.Identity.Refresh", refreshCookie));

            var refreshResponse = await httpClient.PostAsync("/Identity/Gateway/Refresh", null);
            _logger.LogWarning("RefreshToken: Refresh call returned {StatusCode}", (int)refreshResponse.StatusCode);

            if (!refreshResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("RefreshToken: Refresh endpoint returned non-success status {StatusCode}", (int)refreshResponse.StatusCode);
                return TokenValidationResult.Failure();
            }

            var cookies = cookieContainer.GetCookies(httpClient.BaseAddress!);
            var newAccessToken = cookies[".AspNet.Identity"]?.Value;
            var newRefreshToken = cookies[".AspNet.Identity.Refresh"]?.Value;

            if (!string.IsNullOrEmpty(newAccessToken))
            {
                _logger.LogWarning("RefreshToken: Refresh succeeded, new tokens obtained from cookies");
                return TokenValidationResult.Refreshed(newAccessToken, newRefreshToken);
            }

            var refreshBody = await refreshResponse.Content.ReadAsStringAsync();
            _logger.LogWarning("RefreshToken: Refresh response body: {Body}", refreshBody);

            if (!string.IsNullOrEmpty(refreshBody))
            {
                var doc = JsonDocument.Parse(refreshBody);
                var accessToken = doc.RootElement.TryGetProperty("accessToken", out var atEl) && atEl.ValueKind == JsonValueKind.String
                    ? atEl.GetString()
                    : null;
                var refreshToken = doc.RootElement.TryGetProperty("refreshToken", out var rtEl) && rtEl.ValueKind == JsonValueKind.String
                    ? rtEl.GetString()
                    : null;

                if (!string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("RefreshToken: Refresh succeeded, new tokens obtained from response body");
                    return TokenValidationResult.Refreshed(accessToken, refreshToken);
                }
            }

            _logger.LogWarning("RefreshToken: Refresh endpoint returned success but no tokens found in response");
            return TokenValidationResult.Failure();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RefreshToken: exception during Refresh call");
            return TokenValidationResult.Failure();
        }
    }
}
