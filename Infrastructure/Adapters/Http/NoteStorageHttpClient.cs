using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenIddict.Client;
using PikaNoteAPI.Domain.Models.DTO;
using PikaNoteAPI.Infrastructure.Adapters.Http.DTO;

namespace PikaNoteAPI.Infrastructure.Adapters.Http;

public class NoteStorageHttpClient : INoteStorageHttpClient
{
    private readonly IConfiguration _configuration;
    private readonly OpenIddictClientService _openIddictClientService;
    private readonly CookieContainer _cookieContainer;
    
    public NoteStorageHttpClient(
        IConfiguration configuration,
        OpenIddictClientService openIddictClientService
    )
    {
        this._configuration = configuration;
        this._cookieContainer = new CookieContainer();
        this._openIddictClientService = openIddictClientService;
    }

    public async Task<AddNoteResultDTO?> CreateNoteObject(
        string token, 
        string objectName, 
        string bucketId, 
        string humanName,
        string content
        )
    {
        var http = await this.CreateAuthenticatedClient();
        var httpRequest = new HttpRequestMessage
        {
            RequestUri = new Uri(http.BaseAddress?.AbsoluteUri
                                 + $"/NoteStorage?bucketId={bucketId}&objectName={objectName}&token={token}"),
            Method = HttpMethod.Post,
            Content = new StringContent( JsonSerializer.Serialize(new { name=humanName, content }), encoding: Encoding.UTF8, mediaType: "application/json" )

        };
        var response = await http.SendAsync(httpRequest);
        try
        {
            var successResponse = response.EnsureSuccessStatusCode();
            var stringContent = await response.Content.ReadAsStreamAsync();
            var result = await JsonSerializer.DeserializeAsync<AddNoteResultDTO>(
                stringContent
            );
            return result;
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Redirect)
        {
            throw new AuthenticationException("It appears that core system could not authenticate us");
        }
    }
    public async Task<UpdateNoteResultDTO?> UpdateNoteObject(
        string token,
        string objectName,
        string bucketId,
        string humanName,
        string content
        )
    {
        var http = await this.CreateAuthenticatedClient();
        var httpRequest = new HttpRequestMessage
        {
            RequestUri = new Uri(http.BaseAddress?.AbsoluteUri
                                 + $"/NoteStorage?bucketId={bucketId}&objectName={objectName}&token={token}"),
            Method = HttpMethod.Put,
            Content = new StringContent(JsonSerializer.Serialize(new { name = humanName, content }), encoding: Encoding.UTF8, mediaType: "application/json")

        };
        var response = await http.SendAsync(httpRequest);
        try
        {
            var successResponse = response.EnsureSuccessStatusCode();
            var stringContent = await response.Content.ReadAsStreamAsync();
            var result = await JsonSerializer.DeserializeAsync<UpdateNoteResultDTO>(
                stringContent
            );
            return result;
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Redirect)
        {
            throw new AuthenticationException("It appears that core system could not authenticate us");
        }
    }

    public async Task<string?> GetNoteRawContentByIdAsUser(string token, string bucketId, string objectName)
    {
        var http = await this.CreateAuthenticatedClient();
        
        var response = await http.SendAsync(new HttpRequestMessage
        {
            RequestUri = new Uri(http.BaseAddress?.AbsoluteUri
                                 +$"/NoteStorage/Content?bucketId={bucketId}&objectName={objectName}&token={token}"),
            Method = HttpMethod.Get
        });
        try
        {
            var legibleResponse = response.EnsureSuccessStatusCode();
            return await legibleResponse.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException httpRequestException) when (httpRequestException.StatusCode == HttpStatusCode.Redirect)
        {
            return null;
        }
    }

    public async Task<List<BucketDescriptorDTO>?> GetBuckets(string token)
    {
        var http = await this.CreateAuthenticatedClient();

        var response = await http.SendAsync(new HttpRequestMessage
        {
            RequestUri = new Uri(http.BaseAddress?.AbsoluteUri
                                 + $"/NoteStorage/Buckets?token={token}"),
            Method = HttpMethod.Get
        });
        try
        {
            var legibleResponse = response.EnsureSuccessStatusCode();
            var content = await legibleResponse.Content.ReadAsStringAsync();
            var buckets = JsonSerializer.Deserialize<List<BucketDescriptorDTO>>(content);
            return buckets;
        }
        catch (HttpRequestException httpRequestException) when (httpRequestException.StatusCode == HttpStatusCode.Redirect)
        {
            return null;
        }
    }

    private async Task<HttpClient> CreateAuthenticatedClient()
    {
        // TODO: Use cache to store a token.
        var serviceToken = await this._openIddictClientService.AuthenticateWithClientCredentialsAsync(
            new OpenIddictClientModels.ClientCredentialsAuthenticationRequest
            {
                RegistrationId = "base"
            }
        );
        this._cookieContainer.Add(
            new Uri($"{_configuration.GetConnectionString("PikaCoreAPI")}/NoteStorage/Content"), 
            new Cookie(".AspNet.Identity", serviceToken.AccessToken)
        );
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            AllowAutoRedirect = false
        }; 
        var http = new HttpClient(handler);
        http.BaseAddress = new Uri(_configuration.GetConnectionString("PikaCoreAPI"));
        return http;
    }
}