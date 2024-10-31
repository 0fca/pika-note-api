using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using OpenIddict.Client;
using PikaNoteAPI.Domain.Contract;
using PikaNoteAPI.Infrastructure.Services.Security;

namespace PikaNoteAPI.Application.Middlewares;

public class NoteFileStorageSecurity
{
    private readonly RequestDelegate _next;
    private readonly ISecurityService _securityService;
    private readonly OpenIddictClientService _openIddictClientService;
    private readonly INotes _notes;
    private readonly IDistributedCache _cache;
    
    public NoteFileStorageSecurity(
        INotes notes,
        ISecurityService securityService,
        OpenIddictClientService openIddictClientService,
        IDistributedCache cache,
        RequestDelegate next)
    {
        this._securityService = securityService;
        _next = next;
        this._notes = notes;
        this._openIddictClientService = openIddictClientService;
        this._cache = cache;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (!_notes.HasSecurityConfigured())
        {
            var token = await _cache.GetStringAsync("service_token");
            if (string.IsNullOrEmpty(token))
            {
                token = (await _openIddictClientService.AuthenticateWithClientCredentialsAsync(
                    new OpenIddictClientModels.ClientCredentialsAuthenticationRequest
                {
                    RegistrationId = "base"
                })).AccessToken;
                await _cache.SetStringAsync("service_token", token, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                });
            }
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(token);
            var jwst = jsonToken as JwtSecurityToken;
            await _securityService.ConfigureAccessToken(token, jwst.Claims);
            _notes.ConfigureSecurityService(this._securityService);
        }

        await _next(context);
    }
}