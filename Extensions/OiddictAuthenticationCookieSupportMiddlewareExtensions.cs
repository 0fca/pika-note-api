using Microsoft.AspNetCore.Builder;
using PikaNoteAPI.Middlewares;

namespace PikaNoteAPI.Extensions;

public static class OiddictAuthenticationCookieSupportMiddlewareExtensions
{
    public static IApplicationBuilder UseOiddictAuthenticationCookieSupport(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OiddictAuthenticationCookieSupportMiddleware>();
    }
}