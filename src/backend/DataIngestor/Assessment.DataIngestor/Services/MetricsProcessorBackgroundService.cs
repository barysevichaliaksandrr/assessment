using Assessment.DataIngestor.Configuration;
using Microsoft.Extensions.Options;

namespace Assessment.DataIngestor.Services;

public sealed class MetricsProcessorBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<MetricsApiOptions> options,
    ILogger<MetricsProcessorBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        logger.LogInformation(
            "Metrics processor started. Polling {Url} every {Interval}s.",
            settings.BaseUrl,
            settings.PollIntervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.PollIntervalSeconds));

        await RunIngestionCycleAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunIngestionCycleAsync(stoppingToken);
        }
    }

    private async Task RunIngestionCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IMetricsProcessorService>();
            await ingestionService.ProcessAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Metrics processing cycle failed. Will retry on next interval.");
        }
    }
}
