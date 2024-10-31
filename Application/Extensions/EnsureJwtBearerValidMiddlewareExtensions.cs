using Microsoft.AspNetCore.Builder;
using PikaNoteAPI.Application.Middlewares;

namespace PikaNoteAPI.Application.Extensions;

public static class EnsureJwtBearerValidMiddlewareExtensions
{
    public static IApplicationBuilder UseEnsureJwtBearerValid(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<EnsureJwtBearerValidMiddleware>();
    }
}