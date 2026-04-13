# Nac.Messaging.RabbitMQ Implementation

> Status: Complete | Priority: High | Date: 2026-04-12

## Overview

Implement `Nac.Messaging.RabbitMQ` — thin provider package that adds RabbitMQ as a distributed `IEventBus` implementation. Also remove `Nac.Persistence.SqlServer` from deferred list (user decision: not needed).

## Architecture

```
Producer Flow:
  App Code → IEventBus.PublishAsync() → RabbitMqEventBus
    → Serialize to JSON → BasicPublishAsync() → RabbitMQ Exchange (topic)

Consumer Flow:
  RabbitMQ Queue → RabbitMqConsumerWorker (BackgroundService)
    → Deserialize via EventTypeRegistry → IntegrationEventDispatcher
    → IIntegrationEventHandler<TEvent> → Ack/Nack
```

**Key decisions:**
- Follow provider pattern (like `Nac.Persistence.PostgreSQL`)
- `RabbitMQ.Client` v7.x (async-first, .NET 10 compatible)
- Topic exchange for routing by EventType
- `InternalsVisibleTo` for accessing `IntegrationEventDispatcher` + `EventTypeRegistry`
- Singleton connection manager, separate publish/consume channels
- Manual ack with nack+requeue on failure
- Prefetch count configurable (default 10)

## Dependencies

- `RabbitMQ.Client` 7.2.1 (NuGet)
- `Nac.Messaging` (ProjectReference)

## Files to Create

| File | Purpose | ~LOC |
|------|---------|------|
| `Nac.Messaging.RabbitMQ.csproj` | Project file | 15 |
| `RabbitMqOptions.cs` | Configuration POCO | 25 |
| `RabbitMqConnectionManager.cs` | Singleton connection + channel factory | 55 |
| `RabbitMqEventBus.cs` | IEventBus → publish to RabbitMQ | 50 |
| `RabbitMqConsumerWorker.cs` | BackgroundService → consume + dispatch | 85 |
| `Extensions/RabbitMqMessagingExtensions.cs` | DI registration | 45 |

## Files to Modify

| File | Change |
|------|--------|
| `Nac.slnx` | Add project reference |
| `Nac.Messaging.csproj` | Add `InternalsVisibleTo` |

## Phases

- [x] Phase 1: Project setup + InternalsVisibleTo
- [x] Phase 2: RabbitMqOptions + ConnectionManager
- [x] Phase 3: RabbitMqEventBus (publisher)
- [x] Phase 4: RabbitMqConsumerWorker (consumer)
- [x] Phase 5: DI extensions
- [x] Phase 6: Build verification + code review fixes
- [x] Phase 7: Update progress/docs, remove SqlServer from deferred

## Success Criteria

- `dotnet build Nac.slnx` — 0 warnings, 0 errors (15 projects)
- All existing tests still pass
- Clean provider pattern matching PostgreSQL style
