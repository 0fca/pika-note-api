﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace PikaNoteAPI.Application.Controllers;

[Route("[controller]")]
public class SecurityController : Controller
{
    private readonly IConfiguration _configuration;

    public SecurityController(IConfiguration configuration)
    {
        this._configuration = configuration;
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

    [Authorize]
    [Route("[action]")]
    [ActionName("Check")]
    public IActionResult LoginCheck()
    {
        return Ok();
    }
}