using System.Net;
using Assessment.DataIngestor.Services;
using Assessment.DataIngestor.Tests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Assessment.DataIngestor.Tests.Services;

public class MetricsApiClientTests
{
    private static MetricsApiClient CreateClient(HttpMessageHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080") }, NullLogger<MetricsApiClient>.Instance);

    [Fact]
    public async Task GetMetersAsync_DeserializesReadingsFromApi()
    {
        const string json = """
            [
              {"type":"energy","name":"Kitchen","payload":{"energy":55.53}},
              {"type":"motion","name":"Corridor","payload":{"motionDetected":true}}
            ]
            """;

        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(HttpTestResponses.Json(HttpStatusCode.OK, json)));

        var client = CreateClient(handler);

        var readings = await client.GetMetersAsync();

        readings.Count.Should().Be(2);
        readings[0].Type.Should().Be("energy");
        readings[0].Name.Should().Be("Kitchen");
        readings[0].Payload.GetProperty("energy").GetDouble().Should().Be(55.53);
        readings[1].Type.Should().Be("motion");
        readings[1].Payload.GetProperty("motionDetected").GetBoolean().Should().BeTrue();

        var request = handler.Requests.Single();
        request.RequestUri.Should().NotBeNull();
        request.RequestUri.AbsolutePath.Should().Be("/meters");
    }

    [Fact]
    public async Task GetMetersAsync_ReturnsEmptyList_WhenApiReturnsNullBody()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(HttpTestResponses.Json(HttpStatusCode.OK, "null")));

        var client = CreateClient(handler);

        var readings = await client.GetMetersAsync();

        readings.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMetersAsync_Throws_WhenApiReturnsErrorStatus()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)));

        var client = CreateClient(handler);

        var act = () => client.GetMetersAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task IsHealthyAsync_ReturnsTrue_WhenHealthEndpointSucceeds()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            request.RequestUri.Should().NotBeNull();
            request.RequestUri.AbsolutePath.Should().Be("/health");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var client = CreateClient(handler);

        var isHealthy = await client.IsHealthyAsync();

        isHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task IsHealthyAsync_ReturnsFalse_WhenHealthEndpointFails()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var client = CreateClient(handler);

        var isHealthy = await client.IsHealthyAsync();

        isHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task IsHealthyAsync_ReturnsFalse_WhenRequestThrows()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));

        var client = CreateClient(handler);

        var isHealthy = await client.IsHealthyAsync();

        isHealthy.Should().BeFalse();
    }
}
