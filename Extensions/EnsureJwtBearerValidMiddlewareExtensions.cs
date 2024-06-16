using Microsoft.AspNetCore.Builder;
using PikaNoteAPI.Middlewares;

namespace PikaNoteAPI.Extensions;

public static class EnsureJwtBearerValidMiddlewareExtensions
{
    public static IApplicationBuilder UseEnsureJwtBearerValid(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<EnsureJwtBearerValidMiddleware>();
    }
}