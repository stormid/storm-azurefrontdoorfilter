using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Storm.AzureFrontDoorFilter
{
    /// <summary>
    /// Provides support for filtering inbound requests depending on whether the request was sent from a recognised Azure Front Door
    /// </summary>
    /// <remarks>https://docs.microsoft.com/en-us/azure/frontdoor/front-door-faq#how-do-i-lock-down-the-access-to-my-backend-to-only-azure-front-door</remarks>
    public class AzureFrontDoorFilterMiddleware
    {
        private const string ConfigurationSettingName = "AllowedAfdIds";
        public const string AfdIdHeaderName = "x-azure-fdid";
        public const string AfdHealthProbeHeaderName = "x-fd-healthprobe";
        private const string AllowAllValue = "*";

        private readonly RequestDelegate next;
        private readonly ILogger<AzureFrontDoorFilterMiddleware> logger;
        private IList<string> approvedAfdIds = new List<string>() { AllowAllValue };

        public AzureFrontDoorFilterMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<AzureFrontDoorFilterMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
            Configure(configuration);
        }

        private void Configure(IConfiguration configuration)
        {
            try
            {
                var configSection = configuration.GetSection(ConfigurationSettingName);
                if (configSection.Exists())
                {
                    var value = configSection.Get<string>() ?? AllowAllValue;
                    approvedAfdIds = value?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList() ?? Enumerable.Empty<string>().ToList();
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, $"Unable to parse valid configuration for [{ConfigurationSettingName}], defaulting to '{string.Join(";", approvedAfdIds)}'");
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (IsAzureFrontDoorRequest(context.Request))
            {
                await next(context);
            }
            else
            {
                context.Response.StatusCode = 403;
                logger.LogWarning($"Access foridden due to {ConfigurationSettingName} value restrictions");
            }
        }

        private static string? GetHeaderValueOrDefault(IHeaderDictionary headers, string headerName, string? defaultValue = default)
        {
            if (headerName is null)
            {
                throw new ArgumentNullException(nameof(headerName));
            }

            if (headers.TryGetValue(headerName, out var headerValue))
            {
                return headerValue;
            }

            return defaultValue;
        }

        private bool IsAzureFrontDoorRequest(HttpRequest request)
        {
            if (approvedAfdIds.Contains(AllowAllValue))
            {
                return true;
            }

            var afdId = GetHeaderValueOrDefault(request.Headers, AfdIdHeaderName, AllowAllValue);
            return approvedAfdIds.Contains(afdId ?? AllowAllValue);
        }
    }
}
