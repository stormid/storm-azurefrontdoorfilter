using Microsoft.AspNetCore.Builder;

namespace Storm.AzureFrontDoorFilter
{
    public static class AzureFrontDoorFilterMiddlewareExtensions
    {
        public static IApplicationBuilder UseAzureFrontDoorFilter(this IApplicationBuilder app)
        {
            app.UseMiddleware<AzureFrontDoorFilterMiddleware>();
            return app;
        }
    }
}
