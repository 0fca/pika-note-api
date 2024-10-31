using Microsoft.AspNetCore.Builder;
using PikaNoteAPI.Application.Middlewares;

namespace PikaNoteAPI.Application.Extensions;

public static class MapJwtClaimsToIdentityMiddlewareExtensions
{
    public static IApplicationBuilder UseMapJwtClaimsToIdentity(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MapJwtClaimsToIdentityMiddleware>();
    }
}