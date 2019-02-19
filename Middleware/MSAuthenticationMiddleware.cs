using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MockStockBackend.Middleware
{
    public class MSAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        public MSAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // Implement Authentication Here
        public async Task InvokeAsync(HttpContext context)
        {
            // Implement authentication logic here
            Console.WriteLine("Write Authentication Middleware Here!");
            
            // Call the next pipeline item
            await _next(context);
        }
    }
}