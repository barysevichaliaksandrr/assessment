using System.Text.Json;

namespace Assessment.DataIngestor.Models;

public sealed record MeterReadingMessage(
    string Type,
    string Name,
    JsonElement Payload,
    DateTimeOffset IngestedAt,
    string Source = "MetricsApi");
