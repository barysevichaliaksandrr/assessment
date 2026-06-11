using Assessment.DataIngestor.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Assessment.DataIngestor.HealthChecks;

public sealed class MetricsApiHealthCheck(IMetricsApiClient metricsApiClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var isHealthy = await metricsApiClient.IsHealthyAsync(cancellationToken);

        return isHealthy
            ? HealthCheckResult.Healthy("Metrics API is reachable.")
            : HealthCheckResult.Unhealthy("Metrics API is not reachable.");
    }
}
