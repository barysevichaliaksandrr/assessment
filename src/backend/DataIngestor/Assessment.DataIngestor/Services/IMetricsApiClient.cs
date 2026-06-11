using Assessment.DataIngestor.Models;

namespace Assessment.DataIngestor.Services;

public interface IMetricsApiClient
{
    Task<IReadOnlyList<MeterReading>> GetMetersAsync(CancellationToken cancellationToken = default);

    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
