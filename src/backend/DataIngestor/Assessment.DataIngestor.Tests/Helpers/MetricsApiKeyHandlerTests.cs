using Assessment.DataIngestor.Configuration;
using Assessment.DataIngestor.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Assessment.DataIngestor.Tests.Helpers;

public class MetricsApiKeyHandlerTests
{
    [Fact]
    public async Task SendAsync_AddsApiKeyHeaderToRequest()
    {
        var options = Options.Create(new MetricsApiOptions { ApiKey = "supersecret" });
        var innerHandler = new CapturingHandler();
        var handler = new MetricsApiKeyHandler(options)
        {
            InnerHandler = innerHandler
        };

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080") };
        await client.GetAsync("/meters");

        innerHandler.Request.Should().NotBeNull();
        innerHandler.Request.Headers.Should().ContainKey("X-Api-Key");
        innerHandler.Request.Headers.GetValues("X-Api-Key").Single().Should().Be("supersecret");
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
