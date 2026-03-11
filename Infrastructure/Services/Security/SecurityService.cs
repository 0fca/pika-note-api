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

namespace PikaNoteAPI.Infrastructure.Services.Security;

public class SecurityService : ISecurityService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly CookieContainer _cookieContainer;
    
    public SecurityService(
        IConfiguration configuration
        )
    {
        this._configuration = configuration;
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
    
    public async Task<bool> CheckTokenValidityAsync(string? identityCookie, string? refreshCookie)
    {
        if (string.IsNullOrEmpty(identityCookie))
        {
            return false;
        }

        var hasRefreshCookie = !string.IsNullOrEmpty(refreshCookie);

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(identityCookie);
            var jwst = jsonToken as JwtSecurityToken;
            if (jwst == null || (jwst.ValidTo <= DateTime.UtcNow && !hasRefreshCookie))
            {
                return false;
            }
        }
        catch
        {
            return false;
        }

        try
        {
            var cookieContainer = new CookieContainer();
            var httpHandler = new HttpClientHandler { CookieContainer = cookieContainer };
            using var httpClient = new HttpClient(httpHandler)
            {
                BaseAddress = this._http.BaseAddress
            };
            cookieContainer.Add(httpClient.BaseAddress!, new Cookie(".AspNet.Identity", identityCookie));
            if (hasRefreshCookie)
            {
                cookieContainer.Add(httpClient.BaseAddress!, new Cookie(".AspNet.Identity.Refresh", refreshCookie!));
            }
            var response = await httpClient.GetAsync("/Identity/Gateway/Status");
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var isAuthenticated = doc.RootElement.TryGetProperty("isAuthenticated", out var authEl)
                                  && (authEl.ValueKind == JsonValueKind.True || authEl.ValueKind == JsonValueKind.False)
                                  && authEl.GetBoolean();
            var hasUsername = doc.RootElement.TryGetProperty("username", out var userEl)
                             && userEl.ValueKind == JsonValueKind.String
                             && !string.IsNullOrEmpty(userEl.GetString());
            return isAuthenticated && hasUsername;
        }
        catch
        {
            return false;
        }
    }
}