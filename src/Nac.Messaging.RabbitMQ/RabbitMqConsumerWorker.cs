using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nac.Messaging.Internal;
using Nac.Persistence;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Nac.Messaging.RabbitMQ;

/// <summary>
/// Background worker that consumes integration events from a RabbitMQ queue
/// and dispatches them to registered <see cref="Nac.Core.Messaging.IIntegrationEventHandler{TEvent}"/>
/// implementations via <see cref="IntegrationEventDispatcher"/>.
/// Uses manual ack: ack on success, nack without requeue on repeated failure.
/// </summary>
internal sealed class RabbitMqConsumerWorker : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly RabbitMqConnectionManager _connectionManager;
    private readonly RabbitMqOptions _options;
    private readonly EventTypeRegistry _eventTypeRegistry;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqConsumerWorker> _logger;

    public RabbitMqConsumerWorker(
        RabbitMqConnectionManager connectionManager,
        IOptions<RabbitMqOptions> options,
        EventTypeRegistry eventTypeRegistry,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqConsumerWorker> logger)
    {
        _connectionManager = connectionManager;
        _options = options.Value;
        _eventTypeRegistry = eventTypeRegistry;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Wraps the consumer loop with retry logic so a transient RabbitMQ failure
    /// does not crash the entire host application.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunConsumerAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ consumer failed, retrying in {Delay}s",
                    RetryDelay.TotalSeconds);
                await Task.Delay(RetryDelay, ct);
            }
        }
    }

    private async Task RunConsumerAsync(CancellationToken ct)
    {
        var connection = await _connectionManager.GetConnectionAsync(ct);
        var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        try
        {
            // Configure prefetch for flow control
            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: _options.PrefetchCount,
                global: false,
                cancellationToken: ct);

            // Ensure exchange exists (idempotent, matches publisher declaration)
            await channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
                type: ExchangeType.Topic,
                durable: _options.Durable,
                autoDelete: false,
                cancellationToken: ct);

            // Declare consumer queue
            var queueName = string.IsNullOrWhiteSpace(_options.QueueName)
                ? $"nac.{Environment.MachineName}.{Guid.NewGuid().ToString("N")[..8]}"
                : _options.QueueName;

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: _options.Durable,
                exclusive: false,
                autoDelete: false,
                cancellationToken: ct);

            // Bind to all event types — topic exchange routes by EventType
            await channel.QueueBindAsync(
                queue: queueName,
                exchange: _options.ExchangeName,
                routingKey: "#",
                cancellationToken: ct);

            _logger.LogInformation(
                "RabbitMQ consumer started on queue {Queue}, exchange {Exchange}",
                queueName, _options.ExchangeName);

            // Set up async consumer
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(channel, ea);

            await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: ct);

            // Keep running until cancellation
            await Task.Delay(Timeout.Infinite, ct);
        }
        finally
        {
            _logger.LogInformation("RabbitMQ consumer shutting down");
            if (channel.IsOpen)
                await channel.CloseAsync();
            channel.Dispose();
        }
    }

    private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs ea)
    {
        var eventType = ea.BasicProperties.Type;

        try
        {
            // Copy body immediately — ReadOnlyMemory<byte> is only valid in this scope
            var body = ea.Body.ToArray();

            var clrType = ResolveEventType(eventType, body);
            if (clrType is null)
            {
                _logger.LogWarning("Unknown event type {EventType}, acking to discard", eventType);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            var @event = (Nac.Core.Messaging.IIntegrationEvent)
                JsonSerializer.Deserialize(body, clrType)!;

            using var scope = _scopeFactory.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IntegrationEventDispatcher>();
            await dispatcher.DispatchAsync(@event, CancellationToken.None);

            // Persist inbox dedup record if a NacDbContext is registered
            var dbContext = scope.ServiceProvider.GetService<NacDbContext>();
            if (dbContext is not null)
                await dbContext.SaveChangesAsync(CancellationToken.None);

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

            _logger.LogDebug("Dispatched {EventType} ({EventId})",
                @event.EventType, @event.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle message {EventType}", eventType);
            // Don't requeue redelivered messages to avoid infinite poison-message loops
            var requeue = !ea.Redelivered;
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: requeue);
        }
    }

    /// <summary>
    /// Resolves CLR type from EventTypeRegistry (header) or falls back to
    /// parsing the EventType property from JSON payload.
    /// </summary>
    private Type? ResolveEventType(string? headerType, byte[] body)
    {
        // Try header first (set by publisher as BasicProperties.Type)
        if (!string.IsNullOrEmpty(headerType))
        {
            var resolved = _eventTypeRegistry.Resolve(headerType);
            if (resolved is not null) return resolved;
        }

        // Fallback: parse EventType from JSON payload
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("EventType", out var prop))
                return _eventTypeRegistry.Resolve(prop.GetString() ?? "");
        }
        catch (JsonException)
        {
            // Not valid JSON — nothing to resolve
        }

        return null;
    }
}
