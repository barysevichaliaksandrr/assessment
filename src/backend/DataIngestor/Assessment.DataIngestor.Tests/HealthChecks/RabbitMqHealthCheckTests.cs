using Assessment.DataIngestor.HealthChecks;
using Assessment.DataIngestor.Services;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;

namespace Assessment.DataIngestor.Tests.HealthChecks;

public class RabbitMqHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenPublisherIsConnected()
    {
        var publisher = new Mock<IRabbitMqPublisher>();
        publisher.SetupGet(p => p.IsConnected).Returns(true);

        var healthCheck = new RabbitMqHealthCheck(publisher.Object);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenPublisherIsNotConnected()
    {
        var publisher = new Mock<IRabbitMqPublisher>();
        publisher.SetupGet(p => p.IsConnected).Returns(false);

        var healthCheck = new RabbitMqHealthCheck(publisher.Object);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
