using Assessment.DataIngestor.HealthChecks;
using Assessment.DataIngestor.Services;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;

namespace Assessment.DataIngestor.Tests.HealthChecks;

public class MetricsApiHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenApiIsReachable()
    {
        var apiClient = new Mock<IMetricsApiClient>();
        apiClient
            .Setup(client => client.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var healthCheck = new MetricsApiHealthCheck(apiClient.Object);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("reachable", because: "healthy status should describe reachability");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenApiIsNotReachable()
    {
        var apiClient = new Mock<IMetricsApiClient>();
        apiClient
            .Setup(client => client.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var healthCheck = new MetricsApiHealthCheck(apiClient.Object);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
