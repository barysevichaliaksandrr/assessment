using System.Net.Http.Headers;
using Assessment.DataIngestor.Configuration;
using Assessment.DataIngestor.HealthChecks;
using Assessment.DataIngestor.Helpers;
using Assessment.DataIngestor.Services;
using Polly;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.Configure<MetricsApiOptions>(builder.Configuration.GetSection(MetricsApiOptions.SectionName));
    builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

    var metricsApiOptions = builder.Configuration
        .GetSection(MetricsApiOptions.SectionName)
        .Get<MetricsApiOptions>() ?? new MetricsApiOptions();

    builder.Services.AddTransient<MetricsApiKeyHandler>();
    builder.Services.AddHttpClient<IMetricsApiClient, MetricsApiClient>(client =>
        {
            client.BaseAddress = new Uri(metricsApiOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(metricsApiOptions.RequestTimeoutSeconds);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddHttpMessageHandler<MetricsApiKeyHandler>()
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 6;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(metricsApiOptions.RequestTimeoutSeconds);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(2);
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(metricsApiOptions.RequestTimeoutSeconds * 2);
        });

    builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
    builder.Services.AddScoped<IMetricsProcessorService, MetricsProcessorService>();
    builder.Services.AddHostedService<MetricsProcessorBackgroundService>();

    builder.Services.AddOpenApi();
    builder.Services.AddHealthChecks()
        .AddCheck<MetricsApiHealthCheck>("MetricsApi")
        .AddCheck<RabbitMqHealthCheck>("rabbitmq");

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.MapHealthChecks("/health");

    app.MapGet("/info", () => Results.Ok(new
    {
        service = "DataIngestor",
        status = "running",
        description = "Fetches meter data from Metrics API and publishes to RabbitMQ."
    }));

    app.MapPost("/ingest", async (IMetricsProcessorService metricsProcessorService, CancellationToken cancellationToken) =>
    {
        var count = await metricsProcessorService.ProcessAsync(cancellationToken);
        return Results.Ok(new { ingested = count });
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Data Ingestor terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
