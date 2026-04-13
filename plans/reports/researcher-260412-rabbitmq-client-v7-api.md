# RabbitMQ.Client v7.x API Research Report

## Executive Summary

**Latest Version:** 7.2.1 (as of April 2026)
**Status:** Production-ready, fully async-first (TAP model)
**Compatibility:** .NET Standard 2.0+, .NET Framework 4.6.1+, .NET 10 supported

v7 is fundamentally different from v6: entire API redesigned around Task-based async patterns. No synchronous publishing/consuming methods exist. Migration from v6 requires systematic interface renaming and adoption of async/await throughout.

## 1. Breaking Changes v6 → v7

### Critical Renames
- `IModel` → `IChannel`
- `IBasicConsumer` → `IAsyncBasicConsumer`
- All sync methods removed entirely (no fallback)

### API Restructuring
- `CreateBasicProperties()` method removed; instantiate `new BasicProperties()` directly
- All operations use `*Async` methods returning `Task<T>` or `Task`
- Consumer event handlers become async delegates

### Memory Safety (Critical for Your Framework)
- Message body arrives as `ReadOnlyMemory<byte>` (not `byte[]`)
- **Body is owned by library**, only valid during handler execution
- **Must copy immediately**: `byte[] msgBody = eventArgs.Body.ToArray();`
- Retaining reference after handler returns causes memory corruption

---

## 2. Core API Patterns for v7

### Connection & Channel Creation
```csharp
// Factory setup
var factory = new ConnectionFactory 
{ 
    HostName = "localhost",
    Port = 5672,
    UserName = "guest",
    Password = "guest",
    AutomaticRecoveryEnabled = true,
    TopologyRecoveryEnabled = true  // Re-declare exchanges/queues on reconnect
};

// Async connection lifecycle
IConnection conn = await factory.CreateConnectionAsync();
IChannel channel = await conn.CreateChannelAsync();

// Safe cleanup
await channel.CloseAsync();
await conn.CloseAsync();
```

### Exchange & Queue Declaration
```csharp
// Declare topic exchange for events (idempotent)
await channel.ExchangeDeclareAsync(
    exchange: "events.topic",
    type: ExchangeType.Topic,
    durable: true,
    autoDelete: false
);

// Declare queue bound to exchange
await channel.QueueDeclareAsync(
    queue: "my-service-queue",
    durable: true,
    exclusive: false,
    autoDelete: false
);

// Bind with routing key pattern
await channel.QueueBindAsync(
    queue: "my-service-queue",
    exchange: "events.topic",
    routingKey: "domain.event.#"  // Topic pattern
);
```

### Publishing Messages (Async-First)
```csharp
var props = new BasicProperties
{
    ContentType = "application/json",
    ContentEncoding = "utf-8",
    DeliveryMode = 2,  // Persistent delivery
    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
};

// Serialize event to JSON
byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event));

// Publish with routing key
await channel.BasicPublishAsync(
    exchange: "events.topic",
    routingKey: "domain.EventOccurred",
    props,
    body
);
```

---

## 3. Consumer Patterns (Async-First)

### Pattern A: AsyncEventingBasicConsumer (Recommended for Your Use Case)
```csharp
var consumer = new AsyncEventingBasicConsumer(channel);

consumer.ReceivedAsync += async (ch, ea) =>
{
    try
    {
        // CRITICAL: Copy payload immediately - memory only valid in this scope
        byte[] body = ea.Body.ToArray();
        
        // Deserialize JSON
        var @event = JsonSerializer.Deserialize<IIntegrationEvent>(body);
        
        // Use your IntegrationEventDispatcher
        await dispatcher.DispatchAsync(@event, cancellationToken);
        
        // Manual ack
        await channel.BasicAckAsync(ea.DeliveryTag, false);
    }
    catch (Exception ex)
    {
        // Negative ack (requeue)
        await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
    }
};

// Start consuming (blocking queue with 1 concurrent handler)
string consumerTag = await channel.BasicConsumeAsync(
    queue: "my-service-queue",
    autoAck: false,  // Manual acking required
    consumerTag: "",
    consumer: consumer
);
```

### Pattern B: Custom IAsyncBasicConsumer (If You Need Lifecycle Control)
```csharp
public class EventConsumer : AsyncDefaultBasicConsumer
{
    private readonly IntegrationEventDispatcher _dispatcher;
    private readonly CancellationToken _ct;

    public EventConsumer(IChannel channel, IntegrationEventDispatcher dispatcher, CancellationToken ct)
        : base(channel)
    {
        _dispatcher = dispatcher;
        _ct = ct;
    }

    public override async Task HandleBasicDeliverAsync(
        string consumerTag,
        ulong deliveryTag,
        bool redelivered,
        string exchange,
        string routingKey,
        IReadOnlyBasicProperties properties,
        ReadOnlyMemory<byte> body)
    {
        try
        {
            // Copy immediately
            byte[] msgBody = body.ToArray();
            var @event = JsonSerializer.Deserialize<IIntegrationEvent>(msgBody);
            await _dispatcher.DispatchAsync(@event, _ct);
            await Channel.BasicAckAsync(deliveryTag, false);
        }
        catch (Exception ex)
        {
            await Channel.BasicNackAsync(deliveryTag, false, requeue: true);
        }
    }

    public override async Task HandleChannelShutdownAsync(
        IChannel channel,
        ShutdownEventArgs reason)
    {
        // Cleanup on channel shutdown
        await base.HandleChannelShutdownAsync(channel, reason);
    }
}

// Usage
var consumer = new EventConsumer(channel, dispatcher, cancellationToken);
await channel.BasicConsumeAsync("my-service-queue", autoAck: false, consumer: consumer);
```

---

## 4. Connection Resilience & Recovery

### Automatic Recovery Configuration
```csharp
var factory = new ConnectionFactory
{
    HostName = "localhost",
    
    // Enable automatic reconnection
    AutomaticRecoveryEnabled = true,
    
    // Topology recovery (re-declare exchanges/queues/bindings)
    TopologyRecoveryEnabled = true,
    
    // Network recovery interval (seconds)
    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
    
    // Connection timeout
    ContinuationTimeout = TimeSpan.FromSeconds(10),
    
    // Request heartbeat from server
    RequestedHeartbeat = TimeSpan.FromSeconds(30)
};

// Connection recovery automatically handles:
// - Reconnection after network failure
// - Re-declaration of exchanges, queues, bindings
// - Consumer restoration (via TopologyRecoveryEnabled)
```

### Publisher Confirmations (For Guaranteed Delivery)
```csharp
// Enable publisher confirms for this channel
await channel.ConfirmSelectAsync();

// Publish with confirmation tracking
var tcs = new TaskCompletionSource<bool>();

channel.BasicAcks += (ch, ea) =>
{
    // Message confirmed by broker
    tcs.TrySetResult(true);
};

channel.BasicNacks += (ch, ea) =>
{
    // Message rejected by broker
    tcs.TrySetException(new InvalidOperationException("Nack from broker"));
};

await channel.BasicPublishAsync(
    exchange: "events.topic",
    routingKey: "domain.EventOccurred",
    props,
    body
);

// Wait for confirmation
await tcs.Task;
```

---

## 5. Message Serialization (JSON)

### Default Approach: System.Text.Json
```csharp
// Serialize event to JSON
using var json = new MemoryStream();
await JsonSerializer.SerializeAsync(json, @event);
byte[] body = json.ToArray();

// Deserialize from JSON
var @event = JsonSerializer.Deserialize<IIntegrationEvent>(
    body,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
);
```

### With Type Registry (For Polymorphic Events)
```csharp
// In your handler:
byte[] body = ea.Body.ToArray();
string json = Encoding.UTF8.GetString(body);

// Use EventTypeRegistry to resolve actual type
var typeNameProperty = JsonDocument.Parse(json)
    .RootElement.GetProperty("$type").GetString();

Type eventType = eventTypeRegistry.GetType(typeNameProperty);
var @event = JsonSerializer.Deserialize(json, eventType);

await dispatcher.DispatchAsync((IIntegrationEvent)@event, ct);
```

### Optional: Newtonsoft.Json (If Preferred)
```csharp
// Serialize
byte[] body = Encoding.UTF8.GetBytes(
    JsonConvert.SerializeObject(@event)
);

// Deserialize
var @event = JsonConvert.DeserializeObject<IIntegrationEvent>(
    Encoding.UTF8.GetString(body)
);
```

---

## 6. Integration with Your Framework

### Recommended RabbitMQ Provider Implementation Pattern

```csharp
public class RabbitMQEventBus : IEventBus, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _publishChannel;
    private readonly IChannel _consumeChannel;
    private readonly EventTypeRegistry _eventTypeRegistry;
    private readonly IntegrationEventDispatcher _dispatcher;

    // In PublishAsync (matches your IEventBus contract):
    public async Task PublishAsync(IIntegrationEvent @event, CancellationToken ct)
    {
        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = 2  // Persistent
        };

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(@event);

        // Use EventTypeRegistry for routing key
        string routingKey = _eventTypeRegistry.GetRoutingKey(@event.GetType());

        await _publishChannel.BasicPublishAsync(
            exchange: "events.topic",
            routingKey: routingKey,
            props,
            body,
            cancellationToken: ct
        );
    }

    // Consumer setup (call once per event type):
    public async Task SubscribeAsync<TEvent>(CancellationToken ct) 
        where TEvent : IIntegrationEvent
    {
        string queueName = $"{nameof(TEvent)}-queue";
        string routingKey = _eventTypeRegistry.GetRoutingKey(typeof(TEvent));

        await _consumeChannel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct
        );

        await _consumeChannel.QueueBindAsync(
            queue: queueName,
            exchange: "events.topic",
            routingKey: routingKey,
            cancellationToken: ct
        );

        var consumer = new AsyncEventingBasicConsumer(_consumeChannel);
        consumer.ReceivedAsync += HandleMessageAsync;

        await _consumeChannel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct
        );
    }

    private async Task HandleMessageAsync(object ch, BasicDeliverEventArgs ea)
    {
        try
        {
            byte[] body = ea.Body.ToArray();
            var @event = JsonSerializer.Deserialize<IIntegrationEvent>(body);
            
            // Use your IntegrationEventDispatcher
            await _dispatcher.DispatchAsync(@event, CancellationToken.None);
            
            await _consumeChannel.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            // Log error
            await _consumeChannel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
        }
    }
}
```

---

## 7. Thread Safety & Channel Usage

### Critical Constraints
- **Channels**: Cannot share single channel between concurrent publishers
- **Connections**: Thread-safe; can have multiple channels from one connection
- **Consumer Dispatch**: Sequential by default (one handler at a time per queue)

### Thread-Safe Publishing (Multiple Publishers)
```csharp
// Option 1: Use lock for shared channel
private readonly object _publishLock = new();

public async Task PublishAsync(IIntegrationEvent @event, CancellationToken ct)
{
    lock (_publishLock)
    {
        return await _publishChannel.BasicPublishAsync(...);
    }
}

// Option 2: Use per-thread channel from same connection
var channel = await connection.CreateChannelAsync();
// Each publisher thread gets own channel
```

### Concurrent Consumer Handling
```csharp
// Only set this if you want concurrent handlers (NOT recommended for ordered events)
await channel.BasicQosAsync(
    prefetchSize: 0,
    prefetchCount: 1,  // Process one message at a time
    global: false
);
```

---

## 8. Key Differences from v6

| Aspect | v6 | v7 |
|--------|----|----|
| **Interface** | `IModel` | `IChannel` |
| **Async Model** | Mixed sync/async | Pure async (TAP) |
| **Properties Factory** | `CreateBasicProperties()` | `new BasicProperties()` |
| **Consumer Base** | `IBasicConsumer` | `IAsyncBasicConsumer` |
| **Message Body** | `byte[]` | `ReadOnlyMemory<byte>` |
| **Event Consumer** | `EventingBasicConsumer` | `AsyncEventingBasicConsumer` |
| **Thread Model** | Thread-per-channel | .NET Channel (internal) |
| **Recovery** | Manual fallback code | Built-in `AutomaticRecoveryEnabled` |

---

## 9. Adoption Risk Assessment

| Factor | Rating | Notes |
|--------|--------|-------|
| **Maturity** | ✅ Stable | Released Oct 2023, 2+ years in production |
| **Community** | ✅ Large | Official RabbitMQ client, widely adopted |
| **Breaking Changes** | ⚠️ Major | v6→v7 requires systematic refactoring |
| **Documentation** | ✅ Good | Official guide, API docs complete |
| **Support** | ✅ Active | Regular releases, fast issue response |

**Recommendation:** v7.x is production-ready and required for modern .NET 10. Adoption risk is **low** if you're starting greenfield (no v6 migration pain). Your framework's async-first design aligns perfectly with v7's TAP model.

---

## 10. Implementation Priorities for Your Provider

1. **Connection Factory Setup** (handles recovery automatically)
2. **Exchange/Queue Declaration** (idempotent, can run on startup)
3. **AsyncEventingBasicConsumer** (simpler than custom IAsyncBasicConsumer)
4. **Immediate Body Copying** (critical memory safety)
5. **EventTypeRegistry Integration** (routing key + type resolution)
6. **Error Handling** (Nack with requeue on dispatcher failure)
7. **Graceful Shutdown** (close channel, then connection)

---

## Sources

- [RabbitMQ.Client NuGet 7.2.1](https://www.nuget.org/packages/rabbitmq.client/)
- [v7 Migration Guide](https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/main/v7-MIGRATION.md)
- [Official .NET/C# Client API Guide](https://www.rabbitmq.com/client-libraries/dotnet-api-guide)
- [IAsyncBasicConsumer API Docs](https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.IAsyncBasicConsumer.html)
- [AsyncEventingBasicConsumer API Docs](https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.Events.AsyncEventingBasicConsumer.html)
- [GitHub Release Notes & Changelog](https://github.com/rabbitmq/rabbitmq-dotnet-client/releases)

---

## Unresolved Questions

1. Will you use publisher confirms for guaranteed delivery (outbox fallback needed?)?
2. Should consumers be concurrent or sequential per queue (for event ordering)?
3. Any specific RabbitMQ topology naming convention desired (exchange/queue/routing-key patterns)?
4. Need cluster/HA support or single-node sufficient for MVP?
