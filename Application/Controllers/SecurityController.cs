using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PikaNoteAPI.Application.Filters;
using PikaNoteAPI.Infrastructure.Services.Security;

namespace PikaNoteAPI.Application.Controllers;

[Route("[controller]")]
public class SecurityController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ISecurityService _securityService;

    public SecurityController(IConfiguration configuration, ISecurityService securityService)
    {
        this._configuration = configuration;
        this._securityService = securityService;
    }

    [AllowAnonymous]
    [Route("[action]")]
    [HttpPost]
    public IActionResult LocalLogin()
    {
        var redirectUrl = _configuration["RedirectUrl"];
        var callbackUrl = _configuration["CallbackUrl"];
        return Redirect($"{redirectUrl}?returnUrl={callbackUrl}");
    }

    [PikaCoreAuthorize]
    [Route("[action]")]
    [ActionName("Check")]
    public async Task<IActionResult> LoginCheck()
    {
        var identityCookie = HttpContext.Request.Cookies[".AspNet.Identity"];
        var refreshCookie = HttpContext.Request.Cookies[".AspNet.Identity.Refresh"];
        var isValid = await _securityService.CheckTokenValidityAsync(identityCookie, refreshCookie);
        if (!isValid)
        {
            return Unauthorized();
        }
        return Ok();
    }
}