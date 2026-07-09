using System;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PikaNoteAPI.Application.Middlewares;

public class EnsureJwtBearerValidMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EnsureJwtBearerValidMiddleware> _logger;

    public EnsureJwtBearerValidMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string token = context.Request.Cookies[".AspNet.Identity"];
        if (string.IsNullOrEmpty(token))
        {
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader.Substring("Bearer ".Length).Trim();
            }
        }
       

        if (string.IsNullOrEmpty(token))
        {
            await _next(context);
            return;
        }

        var handler = new JwtSecurityTokenHandler();
        var jwst = handler.ReadToken(token) as JwtSecurityToken;

        var expClaim = jwst!.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (expClaim == null || !long.TryParse(expClaim, out var exp))
        {
            _logger.LogWarning("Token is invalid ('exp' claim), returning 401");
            context.Response.Cookies.Delete(".AspNet.Identity", new CookieOptions
            {
                Path = "/",
                Domain = _configuration.GetSection("Keycloak")["CookieDomain"]
            });
            context.Response.Cookies.Delete(".AspNet.Identity.Refresh", new CookieOptions
            {
                Path = "/",
                Domain = _configuration.GetSection("Keycloak")["CookieDomain"]
            });
            context.Response.StatusCode = 401;
            await _next(context);
            return;
        }

        var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var diff = exp - nowUnix;

        _logger.LogDebug("EnsureJwtBearerValid: exp={Exp}, now={Now}, diff={Diff}s", exp, nowUnix, diff);

        if (diff <= 0)
        {
            _logger.LogWarning("Token expired {Elapsed}s ago (exp={Exp}, now={Now}), returning 401", Math.Abs(diff), exp, nowUnix);
            context.Response.Cookies.Delete(".AspNet.Identity", new CookieOptions
            {
                Path = "/",
                Domain = _configuration.GetSection("Keycloak")["CookieDomain"]
            });
            context.Response.StatusCode = 401;
        }

        await _next(context);
    }
}
