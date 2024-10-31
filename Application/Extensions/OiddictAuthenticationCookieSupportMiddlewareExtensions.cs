using Microsoft.AspNetCore.Builder;
using PikaNoteAPI.Application.Middlewares;

namespace PikaNoteAPI.Application.Extensions;

public static class OiddictAuthenticationCookieSupportMiddlewareExtensions
{
    public static IApplicationBuilder UseOiddictAuthenticationCookieSupport(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OiddictAuthenticationCookieSupportMiddleware>();
    }
}