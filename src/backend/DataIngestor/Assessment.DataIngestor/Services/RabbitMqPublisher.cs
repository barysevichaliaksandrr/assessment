using System.Text;
using System.Text.Json;
using Assessment.DataIngestor.Configuration;
using Assessment.DataIngestor.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Assessment.DataIngestor.Services;

public sealed class RabbitMqPublisher : IRabbitMqPublisher, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConnected => _connection?.IsOpen == true && _channel?.IsOpen == true;

    public Task PublishAsync(MeterReadingMessage message, CancellationToken cancellationToken = default)
        => PublishBatchAsync([message], cancellationToken);

    public async Task PublishBatchAsync(IEnumerable<MeterReadingMessage> messages, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        var channel = _channel ?? throw new InvalidOperationException("RabbitMQ channel is not available.");

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        var published = 0;

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));
            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _options.QueueName,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            published++;
        }

        _logger.LogInformation("Published {Count} meter reading(s) to queue '{Queue}'.", published, _options.QueueName);
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (IsConnected)
        {
            return;
        }

        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected)
            {
                return;
            }

            await DisposeResourcesAsync();
            await ConnectAsync(cancellationToken);
        }
        finally
        {
            _connectLock.Release();
        }
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Connected to RabbitMQ at {Host}:{Port}, queue '{Queue}'.",
            _options.Host,
            _options.Port,
            _options.QueueName);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeResourcesAsync();
        _connectLock.Dispose();
    }

    private async Task DisposeResourcesAsync()
    {
        if (_channel is not null)
        {
            try
            {
                await _channel.CloseAsync();
                await _channel.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error closing RabbitMQ channel.");
            }
        }

        if (_connection is not null)
        {
            try
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error closing RabbitMQ connection.");
            }
        }

        _channel = null;
        _connection = null;
    }
}
