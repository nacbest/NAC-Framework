# Messaging Wiring Patterns

## Program.cs — InMemory

```csharp
using Nac.Messaging.Extensions;

// Channel-based in-memory messaging (single-process)
builder.Services.AddNacInMemoryMessaging(
    typeof({Namespace}.{Module}.{Module}Module).Assembly);
```

## Program.cs — Outbox

```csharp
using Nac.Messaging.Extensions;

// Outbox messaging — {Module}DbContext must inherit NacDbContext (from Nac.Persistence)
builder.Services.AddNacOutboxMessaging<{Module}DbContext>(
    typeof({Namespace}.{Module}.{Module}Module).Assembly);
```

## Program.cs — RabbitMQ

```csharp
using Nac.Messaging.Extensions;
using Nac.Messaging.RabbitMQ.Extensions;

// RabbitMQ distributed messaging
builder.Services.AddNacRabbitMQ(
    options => builder.Configuration.GetSection("RabbitMq").Bind(options),
    typeof({Namespace}.{Module}.{Module}Module).Assembly);
```

## appsettings.json — RabbitMQ

```json
{
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "ExchangeName": "nac.events",
    "QueueName": "",
    "PrefetchCount": 10,
    "Durable": true
  }
}
```

## Host.csproj — ProjectReferences

```xml
<!-- InMemory or Outbox -->
<ProjectReference Include="..\..\src\Nac.Messaging\Nac.Messaging.csproj" />

<!-- RabbitMQ (add both) -->
<ProjectReference Include="..\..\src\Nac.Messaging\Nac.Messaging.csproj" />
<ProjectReference Include="..\..\src\Nac.Messaging.RabbitMQ\Nac.Messaging.RabbitMQ.csproj" />
```

## Usage Examples

```csharp
// Define integration event
public sealed record OrderPlacedEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required string CustomerEmail { get; init; }
}

// Publish from command handler
public class PlaceOrderHandler : ICommandHandler<PlaceOrderCommand, Guid>
{
    private readonly IEventBus _eventBus;

    public async Task<Guid> Handle(PlaceOrderCommand cmd, CancellationToken ct)
    {
        // ... create order ...
        await _eventBus.PublishAsync(new OrderPlacedEvent
        {
            OrderId = order.Id,
            CustomerEmail = cmd.Email
        }, ct);
        return order.Id;
    }
}

// Handle in another module (must be idempotent)
public class SendOrderConfirmationHandler
    : IIntegrationEventHandler<OrderPlacedEvent>
{
    public Task HandleAsync(OrderPlacedEvent @event, CancellationToken ct)
    {
        // Send confirmation email...
        return Task.CompletedTask;
    }
}
```
