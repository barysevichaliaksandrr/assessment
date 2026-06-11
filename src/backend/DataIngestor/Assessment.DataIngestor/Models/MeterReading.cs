using System.Text.Json;

namespace Assessment.DataIngestor.Models;

public sealed record MeterReading(string Type, string Name, JsonElement Payload);
