using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Storm.AzureFrontDoorFilter.Tests
{
    public class TestHostFixture
    {
        public async Task<IHost> Prepare(Action<IConfigurationBuilder> configurationBuilder, CancellationToken cancellationToken = default)
        {
            return await new HostBuilder()
                .ConfigureWebHost(webBuilder => webBuilder
                    .UseTestServer()
                    .ConfigureAppConfiguration(configurationBuilder)
                    .Configure(app =>
                    {
                        app.Map("/_health", a => a.Run(async h => await h.Response.WriteAsync("ok")));
                        app.UseAzureFrontDoorFilter();
                        app.Run(async h => await h.Response.WriteAsync("ok"));
                    }))
                .StartAsync(cancellationToken);
        }

        public async Task<IHost> Prepare(dynamic configurationData, CancellationToken cancellationToken = default)
        {
            return await Prepare(builder =>
            {
                var jsonData = JsonConvert.SerializeObject(configurationData, Formatting.None);
                var jsonConfigurationStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonData));
                builder.AddJsonStream(jsonConfigurationStream);
            });
        }

        public async Task<IHost> Prepare(string allowedAfdIds = "*", CancellationToken cancellationToken = default)
        {
            return await Prepare(builder =>
            {
                var configurationData = allowedAfdIds == null ? null : new[] { new KeyValuePair<string, string>("AllowedAfdIds", allowedAfdIds) };
                builder.AddInMemoryCollection(configurationData);
            });
        }
    }

    public class WhenRequestingUrl : IClassFixture<TestHostFixture>
    {
        private const string Wildcard = "*";
        private const string None = "";
        private const string SingleAllowed = "afdid001";
        private const string MultiAllowed = "afdid001;afdid002";

        private readonly TestHostFixture fixture;

        public WhenRequestingUrl(TestHostFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task WithInvalidConfigurationDataValues()
        {
            using var host = await fixture.Prepare(new { AllowedAfdIds = new { Something = "123" } });

            var request = new HttpRequestMessage(HttpMethod.Get, "/");
            var response = await host.GetTestClient().SendAsync(request);
            response.StatusCode.Should().Be(200);
        }

        [Theory]
        [InlineData(null, null, "/", 200)]
        [InlineData(null, "afdid001", "/", 200)]
        [InlineData(null, null, "/_health", 200)]
        [InlineData(null, "afdid001", "/_health", 200)]

        [InlineData(Wildcard, null, "/", 200)]
        [InlineData(Wildcard, "afdid001", "/", 200)]
        [InlineData(Wildcard, null, "/_health", 200)]
        [InlineData(Wildcard, "afdid001", "/_health", 200)]

        [InlineData(None, null, "/", 403)]
        [InlineData(None, "afdid001", "/", 403)]
        [InlineData(None, null, "/_health", 200)]
        [InlineData(None, "afdid001", "/_health", 200)]

        [InlineData(SingleAllowed, null, "/", 403)]
        [InlineData(SingleAllowed, "afdid001", "/", 200)]
        [InlineData(SingleAllowed, null, "/_health", 200)]
        [InlineData(SingleAllowed, "afdid001", "/_health", 200)]

        [InlineData(MultiAllowed, null, "/", 403)]
        [InlineData(MultiAllowed, "afdid001", "/", 200)]
        [InlineData(MultiAllowed, "afdid002", "/", 200)]
        [InlineData(MultiAllowed, "afdid003", "/", 403)]
        [InlineData(MultiAllowed, null, "/_health", 200)]
        [InlineData(MultiAllowed, "afdid001", "/_health", 200)]
        [InlineData(MultiAllowed, "afdid002", "/_health", 200)]
        [InlineData(MultiAllowed, "afdid003", "/_health", 200)]
        public async Task ShouldHaveCorrectResponseStatusCode(string allowedAfdIds, string injectedFdId, string urlPath, int responseCode)
        {
            using var host = await fixture.Prepare(allowedAfdIds);

            var request = new HttpRequestMessage(HttpMethod.Get, urlPath);
            if (!string.IsNullOrWhiteSpace(injectedFdId))
            {
                request.Headers.Add(AzureFrontDoorFilterMiddleware.AfdIdHeaderName, injectedFdId);
            }
            var response = await host.GetTestClient().SendAsync(request);
            response.StatusCode.Should().Be(responseCode);
            
        }
    }
}
