namespace Assessment.DataIngestor.Services;

public interface IMetricsProcessorService
{
    Task<int> ProcessAsync(CancellationToken cancellationToken = default);
}
