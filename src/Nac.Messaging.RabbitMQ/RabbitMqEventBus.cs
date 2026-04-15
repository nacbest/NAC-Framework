using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nac.Core.Messaging;
using RabbitMQ.Client;

namespace Nac.Messaging.RabbitMQ;

/// <summary>
/// <see cref="IEventBus"/> implementation that publishes integration events
/// to a RabbitMQ topic exchange. The routing key is the event's fully-qualified
/// type name (dots as separators), enabling selective consumer bindings.
/// </summary>
internal sealed class RabbitMqEventBus : IEventBus, IAsyncDisposable
{
    private readonly RabbitMqConnectionManager _connectionManager;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly SemaphoreSlim _channelLock = new(1, 1);
    private IChannel? _channel;

    public RabbitMqEventBus(
        RabbitMqConnectionManager connectionManager,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqEventBus> logger)
    {
        _connectionManager = connectionManager;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(IIntegrationEvent @event, CancellationToken ct = default)
    {
        var channel = await GetOrCreateChannelAsync(ct);

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType());

        var props = new BasicProperties
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = @event.EventId.ToString(),
            Timestamp = new AmqpTimestamp(@event.OccurredAt.ToUnixTimeSeconds()),
            Type = @event.EventType,
        };

        // Routing key = fully-qualified type name (e.g. "MyApp.Orders.OrderPlacedEvent")
        await channel.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: @event.EventType,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);

        _logger.LogDebug("Published {EventType} ({EventId}) to exchange {Exchange}",
            @event.EventType, @event.EventId, _options.ExchangeName);
    }

    private async Task<IChannel> GetOrCreateChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await _channelLock.WaitAsync(ct);
        try
        {
            if (_channel is { IsOpen: true })
                return _channel;

            var connection = await _connectionManager.GetConnectionAsync(ct);
            _channel = await connection.CreateChannelAsync(cancellationToken: ct);

            // Ensure exchange exists (idempotent)
            await _channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
                type: ExchangeType.Topic,
                durable: _options.Durable,
                autoDelete: false,
                cancellationToken: ct);

            return _channel;
        }
        finally
        {
            _channelLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_channel is { IsOpen: true })
                await _channel.CloseAsync();
            _channel?.Dispose();
        }
        finally
        {
            _channelLock.Dispose();
        }
    }
}
