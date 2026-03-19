using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    private const string DefaultCookieDomain = ".lukas-bownik.net";
    private static readonly TimeSpan DefaultMaxAge = TimeSpan.FromMinutes(2);

    public SecurityController(IConfiguration configuration, ISecurityService securityService)
    {
        _configuration = configuration;
        _securityService = securityService;
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
    public IActionResult LoginCheck()
    {
        return Ok();
    }

    [AllowAnonymous]
    [Route("[action]")]
    [HttpPost]
    public async Task<IActionResult> Refresh()
    {
        var identityCookie = Request.Cookies[".AspNet.Identity"];
        var refreshCookie = Request.Cookies[".AspNet.Identity.Refresh"];

        if (string.IsNullOrEmpty(identityCookie) && string.IsNullOrEmpty(refreshCookie))
        {
            return Unauthorized();
        }

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
            Response.Cookies.Delete(".AspNet.Identity", deletionOptions);
            Response.Cookies.Delete(".AspNet.Identity.Refresh", deletionOptions);
            return Unauthorized();
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
            Response.Cookies.Append(".AspNet.Identity", result.NewAccessToken, cookieOptions);
            if (!string.IsNullOrEmpty(result.NewRefreshToken))
            {
                Response.Cookies.Append(".AspNet.Identity.Refresh", result.NewRefreshToken, cookieOptions);
            }
        }

        return Ok();
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