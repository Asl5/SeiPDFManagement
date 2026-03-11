using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SeiPDFManagement.Filters
{
    public class ApiKeyFilter : IAsyncActionFilter
    {
        private readonly IConfiguration _config;

        public ApiKeyFilter(IConfiguration config)
        {
            _config = config;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var requiredKey = _config["Security:ApiKey"];
            if (string.IsNullOrEmpty(requiredKey))
            {
                await next();
                return;
            }

            if (!context.HttpContext.Request.Headers
                    .TryGetValue("X-API-KEY", out var providedKey) ||
                providedKey != requiredKey)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            await next();
        }
    }

}
