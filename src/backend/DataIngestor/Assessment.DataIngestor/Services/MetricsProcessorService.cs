using Assessment.DataIngestor.Models;

namespace Assessment.DataIngestor.Services;

public sealed class MetricsProcessorService(
    IMetricsApiClient metricsApiClient,
    IRabbitMqPublisher publisher,
    ILogger<MetricsProcessorService> logger) : IMetricsProcessorService
{
    public async Task<int> ProcessAsync(CancellationToken cancellationToken = default)
    {
        var readings = await metricsApiClient.GetMetersAsync(cancellationToken);

        if (readings.Count == 0)
        {
            logger.LogWarning("Metrics API returned no meter readings.");
            return 0;
        }

        var ingestedAt = DateTimeOffset.UtcNow;
        var messages = readings
            .Select(reading => new MeterReadingMessage(
                reading.Type,
                reading.Name,
                reading.Payload,
                ingestedAt))
            .ToList();

        await publisher.PublishBatchAsync(messages, cancellationToken);

        logger.LogInformation(
            "Processed {Count} meter reading(s) from Metrics API.",
            messages.Count);

        return messages.Count;
    }
}
