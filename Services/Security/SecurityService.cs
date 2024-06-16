using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenIddict.Client;

namespace PikaNoteAPI.Services.Security;

public class SecurityService : ISecurityService
{
    private readonly OpenIddictClientService _client;
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private string? _token;
    
    public SecurityService(
        OpenIddictClientService client, 
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory
        )
    {
        this._client = client;
        this._configuration = configuration;
        this._http = httpClientFactory.CreateClient();
        this._http.BaseAddress = new Uri(_configuration.GetConnectionString("PikaCoreAPI"));
    }

    public async Task VerifyRemoteClientWithClientId(string clientId)
    {
        var result = await _client.AuthenticateWithClientCredentialsAsync(new OpenIddictClientModels.ClientCredentialsAuthenticationRequest
        {
            RegistrationId = "noteapi-dev"
        });
        this._token = result.AccessToken;
    }

    public async Task<Dictionary<Guid, bool>?> HasNotesAccess(Dictionary<Guid, Guid> notesWithBid)
    {
        this._http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(this._token);
        var content = JsonSerializer.Serialize(notesWithBid);
        var response = await this._http.SendAsync(new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            Content = new StringContent(content, Encoding.UTF8)
        });
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = JsonSerializer.Deserialize<Dictionary<Guid, bool>>(await response.Content.ReadAsStringAsync());
        return result;
    }
}