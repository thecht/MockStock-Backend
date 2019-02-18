using System;
using Microsoft.AspNetCore.Builder;

namespace MockStockBackend.Middleware
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseMSAuthenticationMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MSAuthenticationMiddleware>();
        }
    }
}