using System.Text.Json;
using Assessment.DataIngestor.Models;

namespace Assessment.DataIngestor.Services;

public sealed class MetricsApiClient(
    HttpClient httpClient,
    ILogger<MetricsApiClient> logger) : IMetricsApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<MeterReading>> GetMetersAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("/meters", cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var readings = await JsonSerializer.DeserializeAsync<List<MeterReading>>(stream, JsonOptions, cancellationToken);

        return readings ?? [];
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Metrics API health check failed.");
            return false;
        }
    }
}
