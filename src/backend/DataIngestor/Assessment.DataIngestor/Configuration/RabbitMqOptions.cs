namespace Assessment.DataIngestor.Configuration;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string Username { get; init; } = "guest";

    public string Password { get; init; } = "guest";

    public string QueueName { get; init; } = "meter.readings";
}
