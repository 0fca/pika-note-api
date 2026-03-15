using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PikaNoteAPI.Application.Extensions.Enums;

namespace PikaNoteAPI.Application.Middlewares;

public class MapJwtClaimsToIdentityMiddleware
{
    private readonly RequestDelegate _next;

    public MapJwtClaimsToIdentityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Cookies[".AspNet.Identity"];
        if (string.IsNullOrEmpty(token))
        {
            await _next(context);
            return;
        }
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(token);
        var jwst = jsonToken as JwtSecurityToken;

        var identity = new ClaimsIdentity("PikaCore");
        foreach (var claim in jwst!.Claims)
        {
            if (claim.Type is KeycloakClaimTypes.RealmAccess)
            {
                var roles = JsonSerializer.Deserialize<Dictionary<string, IList<string>>>(claim.Value)!["roles"];
                foreach (var role in roles)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }
            else
            {
                identity.AddClaim(new Claim(claim.Type, claim.Value, claim.ValueType));
            }
        }
        context.User = new ClaimsPrincipal(identity);

        await _next(context);
    }
}