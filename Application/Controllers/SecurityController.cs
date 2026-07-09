using System;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PikaNoteAPI.Application.Filters;
using PikaNoteAPI.Infrastructure.Services.Security;
using System.Threading.Tasks;

namespace PikaNoteAPI.Application.Controllers;

[Route("[controller]")]
public class SecurityController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ISecurityService _securityService;
    private const string DefaultCookieDomain = ".lukas-bownik.net";
    private static readonly TimeSpan DefaultMaxAge = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan DefaultRefreshMaxAge = TimeSpan.FromDays(5);

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

        var result = await _securityService.RefreshTokenAsync(identityCookie, refreshCookie);
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
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Domain = _configuration["CookieDomain"] ?? DefaultCookieDomain,
                MaxAge = GetTokenLifetime(result.NewAccessToken)
            };
            var cookieRefreshOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Domain = _configuration["CookieDomain"] ?? DefaultCookieDomain,
                MaxAge = GetTokenLifetime(result.NewRefreshToken) ?? DefaultRefreshMaxAge
            };
            Response.Cookies.Append(".AspNet.Identity", result.NewAccessToken, cookieOptions);
            if (!string.IsNullOrEmpty(result.NewRefreshToken))
            {
                Response.Cookies.Append(".AspNet.Identity.Refresh", result.NewRefreshToken, cookieRefreshOptions);
            }
        }

        return Ok();
    }

    private static TimeSpan? GetTokenLifetime(string accessToken, int cookieBufferSeconds = 120)
    {
        if (cookieBufferSeconds <= 0)
        {
            cookieBufferSeconds = 120;
        }

        TimeSpan fallback = TimeSpan.FromSeconds(cookieBufferSeconds);
        System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwt;
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            if (handler.ReadToken(accessToken) is not System.IdentityModel.Tokens.Jwt.JwtSecurityToken parsedToken)
            {
                return fallback;
            }

            jwt = parsedToken;
        }
        catch (ArgumentException)
        {
            return fallback;
        }

        var iatClaim = jwt.Claims.FirstOrDefault(c => c.Type == "iat")?.Value;
        var expClaim = jwt.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (!long.TryParse(iatClaim, out long iat) || !long.TryParse(expClaim, out long exp))
        {
            return fallback;
        }

        long keycloakCookieLifetime = exp - iat;
        if (keycloakCookieLifetime <= 0)
        {
            return fallback;
        }

        return TimeSpan.FromSeconds(keycloakCookieLifetime);
    }
}
