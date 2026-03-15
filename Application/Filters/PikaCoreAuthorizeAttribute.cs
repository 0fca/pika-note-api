using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using PikaNoteAPI.Infrastructure.Services.Security;

namespace PikaNoteAPI.Application.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class PikaCoreAuthorizeAttribute : TypeFilterAttribute
{
    public PikaCoreAuthorizeAttribute() : base(typeof(PikaCoreAuthorizationFilter))
    {}
}

public class PikaCoreAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly ISecurityService _securityService;
    private readonly IConfiguration _configuration;
    private const string DefaultCookieDomain = ".lukas-bownik.net";
    private static readonly TimeSpan DefaultMaxAge = TimeSpan.FromMinutes(2);

    public PikaCoreAuthorizationFilter(ISecurityService securityService, IConfiguration configuration)
    {
        _securityService = securityService;
        _configuration = configuration;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var identityCookie = context.HttpContext.Request.Cookies[".AspNet.Identity"];
        var refreshCookie = context.HttpContext.Request.Cookies[".AspNet.Identity.Refresh"];

        var result = await _securityService.CheckTokenValidityAsync(identityCookie, refreshCookie);
        if (!result.IsValid)
        {
            var deletionOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Domain = _configuration["CookieDomain"] ?? DefaultCookieDomain
            };
            context.HttpContext.Response.Cookies.Delete(".AspNet.Identity", deletionOptions);
            context.HttpContext.Response.Cookies.Delete(".AspNet.Identity.Refresh", deletionOptions);
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!string.IsNullOrEmpty(result.NewAccessToken))
        {
            var maxAge = GetTokenLifetime(result.NewAccessToken) ?? DefaultMaxAge;
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Domain = _configuration["CookieDomain"] ?? DefaultCookieDomain,
                MaxAge = maxAge
            };
            context.HttpContext.Response.Cookies.Append(".AspNet.Identity", result.NewAccessToken, cookieOptions);
            if (!string.IsNullOrEmpty(result.NewRefreshToken))
            {
                context.HttpContext.Response.Cookies.Append(".AspNet.Identity.Refresh", result.NewRefreshToken, cookieOptions);
            }
        }
    }

    private static TimeSpan? GetTokenLifetime(string accessToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadToken(accessToken) as JwtSecurityToken;
            if (jwt == null) return null;
            var lifetime = jwt.ValidTo - jwt.ValidFrom;
            return lifetime > TimeSpan.Zero ? lifetime : null;
        }
        catch
        {
            return null;
        }
    }
}
