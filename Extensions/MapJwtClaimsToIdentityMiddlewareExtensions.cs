using Microsoft.AspNetCore.Builder;
using PikaNoteAPI.Middlewares;

namespace PikaNoteAPI.Extensions;

public static class MapJwtClaimsToIdentityMiddlewareExtensions
{
    public static IApplicationBuilder UseMapJwtClaimsToIdentity(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MapJwtClaimsToIdentityMiddleware>();
    }
}