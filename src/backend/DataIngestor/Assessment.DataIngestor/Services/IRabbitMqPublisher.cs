using Assessment.DataIngestor.Models;

namespace Assessment.DataIngestor.Services;

public interface IRabbitMqPublisher
{
    Task PublishAsync(MeterReadingMessage message, CancellationToken cancellationToken = default);

    Task PublishBatchAsync(IEnumerable<MeterReadingMessage> messages, CancellationToken cancellationToken = default);

    bool IsConnected { get; }
}
