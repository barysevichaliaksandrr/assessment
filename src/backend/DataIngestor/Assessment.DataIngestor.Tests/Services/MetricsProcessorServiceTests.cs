using System.Text.Json;
using Assessment.DataIngestor.Models;
using Assessment.DataIngestor.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Assessment.DataIngestor.Tests.Services;

public class MetricsProcessorServiceTests
{
    [Fact]
    public async Task ProcessAsync_ReturnsZero_WhenNoReadingsReturned()
    {
        var apiClient = new Mock<IMetricsApiClient>();
        apiClient
            .Setup(client => client.GetMetersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var publisher = new Mock<IRabbitMqPublisher>();

        var service = new MetricsProcessorService(
            apiClient.Object,
            publisher.Object,
            NullLogger<MetricsProcessorService>.Instance);

        var count = await service.ProcessAsync();

        count.Should().Be(0);
        publisher.Verify(
            p => p.PublishBatchAsync(
                It.IsAny<IEnumerable<MeterReadingMessage>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_PublishesMappedMessages_AndReturnsCount()
    {
        using var payload = JsonDocument.Parse("""{"energy":42.5}""");

        var readings = new List<MeterReading>
        {
            new("energy", "Office", payload.RootElement.Clone())
        };

        var apiClient = new Mock<IMetricsApiClient>();
        apiClient
            .Setup(client => client.GetMetersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        List<MeterReadingMessage>? publishedMessages = null;

        var publisher = new Mock<IRabbitMqPublisher>();
        publisher
            .Setup(p => p.PublishBatchAsync(
                It.IsAny<IEnumerable<MeterReadingMessage>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<MeterReadingMessage>, CancellationToken>((messages, _) =>
                publishedMessages = messages.ToList())
            .Returns(Task.CompletedTask);

        var service = new MetricsProcessorService(
            apiClient.Object,
            publisher.Object,
            NullLogger<MetricsProcessorService>.Instance);

        var count = await service.ProcessAsync();

        count.Should().Be(1);
        publishedMessages.Should().NotBeNull();
        var message = publishedMessages.Should().ContainSingle().Subject;
        message.Type.Should().Be("energy");
        message.Name.Should().Be("Office");
        message.Payload.GetProperty("energy").GetDouble().Should().Be(42.5);
        message.Source.Should().Be("MetricsApi");
        message.IngestedAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ProcessAsync_PropagatesException_WhenApiClientFails()
    {
        var apiClient = new Mock<IMetricsApiClient>();
        apiClient
            .Setup(client => client.GetMetersAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API unavailable"));

        var publisher = new Mock<IRabbitMqPublisher>();

        var service = new MetricsProcessorService(
            apiClient.Object,
            publisher.Object,
            NullLogger<MetricsProcessorService>.Instance);

        var act = () => service.ProcessAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
