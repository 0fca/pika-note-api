using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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

    public PikaCoreAuthorizationFilter(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var identityCookie = context.HttpContext.Request.Cookies[".AspNet.Identity"];
        var refreshCookie = context.HttpContext.Request.Cookies[".AspNet.Identity.Refresh"];

        var result = await _securityService.CheckTokenValidityAsync(identityCookie, refreshCookie);
        if (!result.IsValid)
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
