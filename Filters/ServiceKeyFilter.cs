using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SeiPDFManagement.Filters
{
    public class ServiceKeyFilter(IConfiguration config) : IAsyncActionFilter
    {
        private readonly IConfiguration _config = config;

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var path = context.HttpContext.Request.Path;

            // Applica SOLO alle API
            if (!path.StartsWithSegments("/api"))
            {
                await next();
                return;
            }

            if (!context.HttpContext.Request.Headers.TryGetValue(
                    "X-SEIPDF-KEY", out var key))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (key != _config["ServiceApiKey"])
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            await next();
        }
    }

}
