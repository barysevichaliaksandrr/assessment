using Assessment.DataIngestor.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Assessment.DataIngestor.HealthChecks;

public sealed class RabbitMqHealthCheck(IRabbitMqPublisher publisher) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            publisher.IsConnected
                ? HealthCheckResult.Healthy("RabbitMQ connection is open.")
                : HealthCheckResult.Unhealthy("RabbitMQ connection is not open."));
    }
}
