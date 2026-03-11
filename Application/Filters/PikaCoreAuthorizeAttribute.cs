using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
    private readonly string _roles;

    public PikaCoreAuthorizationFilter(ISecurityService securityService, string roles)
    {
        _securityService = securityService;
        _roles = roles;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var identityCookie = context.HttpContext.Request.Cookies[".AspNet.Identity"];
        var refreshCookie = context.HttpContext.Request.Cookies[".AspNet.Identity.Refresh"];

        var isValid = await _securityService.CheckTokenValidityAsync(identityCookie, refreshCookie);
        if (!isValid)
        {
            context.Result = new UnauthorizedResult();
            return;
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
