﻿using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace PikaNoteAPI.Application.Middlewares;

public class EnsureJwtBearerValidMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public EnsureJwtBearerValidMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var gatewayUrl = _configuration.GetConnectionString("PikaCoreGateway");
        var token = context.Request.Cookies[".AspNet.Identity"];
        if (string.IsNullOrEmpty(token))
        {
            await _next(context);
            return;
        }
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(token);
        var jwst = jsonToken as JwtSecurityToken;
        var validTo = jwst!.ValidTo.ToLocalTime();
        var localNow = DateTime.Now.ToLocalTime();
        
        if (validTo <= localNow)
        {
            var returnUrl = context.Request.Path;
            context.Response.Cookies.Delete(".AspNet.Identity");
            context.Response.Redirect($"{gatewayUrl}?returnUrl={returnUrl}");
            return;
        }
        await _next(context);
    }
}