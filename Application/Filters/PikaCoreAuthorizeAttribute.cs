using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
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
    {
        Arguments = new object[] { string.Empty };
    }

    public string Roles
    {
        get => (string)Arguments[0];
        set => Arguments = new object[] { value };
    }
}

public class PikaCoreAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly ISecurityService _securityService;
    private readonly IConfiguration _configuration;
    private readonly string _roles;
    private const string DefaultCookieDomain = ".lukas-bownik.net";
    private static readonly TimeSpan DefaultMaxAge = TimeSpan.FromMinutes(2);

    public PikaCoreAuthorizationFilter(ISecurityService securityService, IConfiguration configuration, string roles)
    {
        _securityService = securityService;
        _configuration = configuration;
        _roles = roles;
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
                SameSite = SameSiteMode.None,
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
                SameSite = SameSiteMode.None,
                Domain = _configuration["CookieDomain"] ?? DefaultCookieDomain,
                MaxAge = maxAge
            };
            context.HttpContext.Response.Cookies.Append(".AspNet.Identity", result.NewAccessToken, cookieOptions);
            if (!string.IsNullOrEmpty(result.NewRefreshToken))
            {
                context.HttpContext.Response.Cookies.Append(".AspNet.Identity.Refresh", result.NewRefreshToken, cookieOptions);
            }
            identityCookie = result.NewAccessToken;
        }

        if (!string.IsNullOrEmpty(_roles))
        {
            var requiredRoles = _roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var userRoles = GetUserRoles(context, identityCookie);
            if (!requiredRoles.Any(required => userRoles.Contains(required, StringComparer.OrdinalIgnoreCase)))
            {
                context.Result = new ForbidResult();
                return;
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

    private static string[] GetUserRoles(AuthorizationFilterContext context, string? identityCookie)
    {
        var roleClaims = context.HttpContext.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();
        if (roleClaims.Length > 0)
        {
            return roleClaims;
        }

        if (string.IsNullOrEmpty(identityCookie))
        {
            return Array.Empty<string>();
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(identityCookie) as JwtSecurityToken;
            var realmAccess = jsonToken?.Claims.FirstOrDefault(c => c.Type == "realm_access")?.Value;
            if (string.IsNullOrEmpty(realmAccess))
            {
                return Array.Empty<string>();
            }

            using var doc = JsonDocument.Parse(realmAccess);
            if (doc.RootElement.TryGetProperty("roles", out var rolesElement) &&
                rolesElement.ValueKind == JsonValueKind.Array)
            {
                return rolesElement.EnumerateArray()
                    .Select(r => r.GetString() ?? string.Empty)
                    .Where(r => !string.IsNullOrEmpty(r))
                    .ToArray();
            }
        }
        catch
        {
            // Ignored - return empty roles
        }

        return Array.Empty<string>();
    }
}
