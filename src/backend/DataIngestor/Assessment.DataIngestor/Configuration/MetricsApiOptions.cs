namespace Assessment.DataIngestor.Configuration;

public sealed class MetricsApiOptions
{
    public const string SectionName = "MetricsApi";

    public string BaseUrl { get; init; } = "http://localhost:8080";

    public string ApiKey { get; init; } = "supersecret";

    public int PollIntervalSeconds { get; init; } = 30;

    public int RequestTimeoutSeconds { get; init; } = 30;
}
