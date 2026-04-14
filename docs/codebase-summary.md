# NAC Framework — Codebase Summary

Comprehensive package-by-package breakdown of the 15 NuGet packages in the NAC Framework.

---

## 1. Nac.Abstractions

**Purpose:** Zero-dependency interfaces and markers.

**Files:** 23 (auth, caching, exceptions, extensions, messaging, modularity, multi-tenancy, persistence)

**Key Types:**
- `ICommand<T>`, `ICommand` — CQRS command markers (no common base)
- `IQuery<T>` — read-only query marker
- `INotification` — in-process pub/sub marker
- `IRepository<T>`, `IReadRepository<T>`, `IUnitOfWork` — persistence contracts
- `IEventBus`, `IIntegrationEvent`, `IIntegrationEventHandler` — messaging contracts
- `ITenantContext`, `ITenantResolver`, `TenantInfo` — multi-tenancy
- `ICurrentUser`, `IRequirePermission` — auth markers
- `ICacheable`, `ICacheInvalidator` — caching contracts
- `IAuditable`, `ITransactional` — behavioral markers
- `INacModule`, `NacFrameworkBuilder` — module registration
- `Specification<T>` — query specification pattern
- `NacException` — base framework exception (derives from System.Exception)

**Dependencies:** None (ASP.NET Core framework ref only)

**LOC:** ~620

**Notes:** This is the contract layer. Everything else depends on this. Zero-dependency guarantee maintained strictly.

---

## 2. Nac.Domain

**Purpose:** Domain model base classes and aggregates.

**Files:** 9 (entities, domain events, enums, value objects)

**Key Types:**
- `Entity<TId>` — base entity with `Id`, domain event collection, ID-based equality
- `AggregateRoot<TId>` — transactional boundary, optimistic concurrency (`Version: uint`)
- `ValueObject` — immutable, component-based equality (abstract)
- `DomainEvent` — record with `EventId`, `OccurredAt`, inherits from `INotification`
- `Enumeration<TValue>` — strongly-typed enum base
- `StronglyTypedId` — ID generator pattern
- `IHasDomainEvents`, `IAuditableEntity`, `ISoftDeletable` — entity contracts

**Dependencies:** Nac.Abstractions only

**LOC:** ~282

**Notes:** Minimal but powerful. Handlers never call `SaveChanges`—UnitOfWork does.

---

## 3. Nac.Mediator

**Purpose:** Custom CQRS mediator with pipeline behaviors.

**Files:** 19 (abstractions, core, internal, registration)

**Key Types:**

**Core API:**
- `IMediator` — interface: `Send(ICommand)`, `Send<T>(ICommand<T>)`, `Send<T>(IQuery<T>)`, `PublishAsync(INotification)`
- `ICommandHandler<T>`, `ICommandHandler<T, TResult>` — implement for commands
- `IQueryHandler<T, TResult>` — implement for queries
- `INotificationHandler<T>` — implement for domain events
- `ICommandBehavior<T>`, `IQueryBehavior<T>` — separate pipeline middlewares

**Internal:**
- `NacMediator` — orchestrator, delegates to wrappers
- `CommandWrapper`, `QueryWrapper`, `VoidCommandWrapper`, `NotificationWrapper` — type-erased dispatch
- `RequestDelegates`, `Unit` — pipeline delegates and void result

**Registration:**
- `HandlerRegistry` — fail-fast validation; 1 handler per command; dictionary: Type → HandlerFactory
- `HandlerScanner` — reflection-based discovery (assembly scanning)
- `MediatorOptions` — pipeline configuration
- `ServiceCollectionExtensions` — DI registration

**Pipeline Order (default):**

Command: ExceptionHandling → Logging → Validation → Authorization → TenantEnrichment → UnitOfWork → Handler

Query: ExceptionHandling → Logging → Validation → Authorization → Caching → Handler

**Dependencies:** Nac.Abstractions only

**LOC:** ~760

**Notes:** Handler registry is built at startup; missing handler = fail-fast. Behaviors are ordered explicitly, not auto-discovered.

---

## 4. Nac.Persistence

**Purpose:** EF Core integration, Unit of Work, Repository, Outbox/Inbox.

**Files:** 11 (contexts, repositories, UnitOfWork, outbox/inbox, conventions)

**Key Types:**

**Core:**
- `NacDbContext` — abstract base, audit trails support, soft-delete, domain event collection/dispatch
- `EfRepository<TEntity>` — specification-based queries, no IQueryable exposure
- `EfUnitOfWork<TContext>` — transaction management, SaveChanges, domain event dispatch post-commit
- `SpecificationEvaluator` — translates `Specification<T>` to LINQ

**Patterns:**
- `OutboxMessage`, `InboxMessage` — entities for reliable delivery
- `OutboxMessageConfiguration`, `InboxMessageConfiguration` — EF mappings
- `SoftDeleteQueryFilterConvention` — auto-apply WHERE clause for soft-delete

**Behaviors:**
- `UnitOfWorkBehavior` — mediator behavior wrapping command handler in transaction

**Dependencies:** EF Core Relational 10.0.5, Nac.Abstractions, Nac.Domain, Nac.Mediator

**LOC:** ~555

**Notes:** Repository never exposes IQueryable. Queries use Specification pattern. DbContext per module is enforced by CLI.

---

## 5. Nac.Persistence.PostgreSQL

**Purpose:** PostgreSQL-specific provider wrapper.

**Files:** 1

**Key Types:**
- `PostgreSqlPersistenceExtensions` — fluent method: `AddNacPostgreSQL<TContext>()`

**Dependencies:** Nac.Persistence, Npgsql

**LOC:** ~30

**Notes:** Thin wrapper combining persistence setup + Npgsql. Other databases (SQL Server, etc.) would be separate packages.

---

## 6. Nac.Messaging

**Purpose:** Event bus abstraction, in-memory implementation, Outbox/Inbox patterns.

**Files:** 7 (event bus, in-memory, outbox, internal utilities)

**Key Types:**

**Abstraction:**
- `IEventBus` — interface: `PublishAsync<T>(T @event)` (T : IIntegrationEvent)

**In-Memory Implementation:**
- `InMemoryEventBus` — Channel-based, fires-and-forgets async, single process
- `InMemoryEventBusWorker` — BackgroundService, drains event queue, invokes handlers

**Distributed/Outbox Pattern:**
- `OutboxEventBus<TContext>` — writes events to OutboxMessage table in same transaction
- `OutboxWorker<TContext>` — BackgroundService, polls OutboxMessages, publishes to actual IEventBus, marks processed

**Utilities:**
- `EventTypeRegistry` — CLR type → handler mapping
- `IntegrationEventDispatcher` — resolves and invokes handlers by event type
- `MessagingServiceCollectionExtensions` — DI registration

**Dependencies:** Nac.Abstractions, Nac.Persistence, ASP.NET Core

**LOC:** ~486

**Notes:** Swap EventBus at DI level. **OutboxWorker timing:** 5s poll interval, 50-message batch, 10 retries. Configurable via options at DI registration.

---

## 7. Nac.Messaging.RabbitMQ

**Purpose:** RabbitMQ IEventBus implementation.

**Files:** 5 (event bus, connection manager, consumer worker, options, extensions)

**Key Types:**

**Core:**
- `RabbitMqEventBus` — IEventBus impl., topic exchange, JSON serialization, lazy channels
- `RabbitMqConnectionManager` — singleton, auto-recovery, heartbeat (30s default)
- `RabbitMqConsumerWorker` — BackgroundService, manual ACK, prefetch 10, exponential backoff retries
- `RabbitMqOptions` — config POCO: hostname, port, username, virtualhost, exchange, queue naming

**Dependencies:** RabbitMQ.Client 7.2.1, Nac.Messaging, Nac.Persistence, Nac.Abstractions

**LOC:** ~476

**Notes:** Idempotency via event ID deduplication. Consumer retries failed messages to dead-letter queue. Connection lazy-initialized.

---

## 8. Nac.MultiTenancy

**Purpose:** Tenant resolution, strategies, provisioning.

**Files:** 6 (context, resolvers, store, middleware, extensions)

**Key Types:**

**Core:**
- `TenantContext` — scoped tenant state: `TenantId`, `IsMultiTenant`, `Name`
- `ITenantStore` — lookup tenant metadata by ID
- `InMemoryTenantStore` — in-memory store for development

**Resolution (Chain of Responsibility):**
- `ITenantResolver` — interface for custom resolvers
- `HeaderTenantResolver` — reads from HTTP header (X-Tenant-ID)
- Future: SubdomainTenantResolver, ClaimTenantResolver, QueryStringTenantResolver

**Middleware:**
- `TenantResolutionMiddleware` — HTTP pipeline, chains resolvers in order, sets TenantContext

**Configuration:**
- `MultiTenancyServiceCollectionExtensions` — fluent builder

**Dependencies:** Nac.Abstractions, ASP.NET Core

**LOC:** ~280

**Notes:** When multitenancy disabled, `IsMultiTenant = false` = zero overhead. Strategies (Discriminator, Schema, Database) implemented at DbContext level, not here.

---

## 9. Nac.Caching

**Purpose:** Query-level distributed caching with invalidation.

**Files:** 3 (behaviors, extensions)

**Key Types:**
- `CachingQueryBehavior` — checks cache before handler, stores after handler (if `ICacheable`)
- `CacheInvalidationBehavior` — post-command, invalidates cache keys from command's `ICacheInvalidator`
- `CachingServiceCollectionExtensions` — DI registration

**Cache Abstraction:**
- Uses `IDistributedCache` (ASP.NET Core)—swap provider via DI (in-memory, Redis, etc.)

**Parameters:**
- Default TTL: 5 minutes (configurable per query via `ICacheable.Expiry`)
- Cache key: query-provided (via `ICacheable.CacheKey`)

**Dependencies:** Nac.Abstractions, Nac.Mediator

**LOC:** ~100

**Notes:** Query must implement `ICacheable` to be cached. Command must implement `ICacheInvalidator` to invalidate related keys.

---

## 11. Nac.Observability

**Purpose:** Structured logging behaviors.

**Files:** 2 (behaviors, extensions)

**Key Types:**
- `LoggingCommandBehavior` — logs command entry, execution time, result/error
- `LoggingQueryBehavior` — logs query entry, execution time, result/error
- `ObservabilityServiceCollectionExtensions` — DI registration

**Logged Data:**
- Command/Query name, parameters (serialized)
- Execution duration (ms)
- Success/failure + exception details
- Correlation ID (from HttpContext)

**Dependencies:** Nac.Abstractions, Nac.Mediator

**LOC:** ~121

**Notes:** Uses `ILogger<T>`. Correlation ID propagates from HTTP request. Structured logging (ILogger.LogInformation) for easy machine parsing.

---

## 12. Nac.WebApi

**Purpose:** Response envelopes, global exception handler.

**Files:** 3 (response types, exception handler, extensions)

**Key Types:**

**Response Envelopes:**
- `ApiResponse<T>` — record: `Data: T`, `Meta: ResponseMeta`
- `PaginatedResponse<T>` — record: `Data: T[]`, `Pagination`, `Meta`
- `ErrorResponse` — record: `Error`, `Meta` (no data)

**Exception Handler:**
- `GlobalExceptionHandler` — ASP.NET Core exception handler middleware
  - Maps NacException subtypes to HTTP status
  - Logs correlation ID
  - No stack trace in response (production-safe)

**Status Mapping:**
- ValidationException → 400
- UnauthorizedException → 401
- ForbiddenException → 403
- NotFoundException → 404
- ConflictException → 409
- DomainException → 422
- Unhandled → 500 (includes correlation ID for tracing)

**Dependencies:** Nac.Abstractions, ASP.NET Core

**LOC:** ~110

**Notes:** Envelope standardized across all endpoints. Correlation ID included in all error responses.

---

## 13. Nac.Testing

**Purpose:** Test utilities and fake implementations.

**Files:** 3 (fakes, extensions)

**Key Types:**
- `FakeEventBus` — in-memory event bus capture for assertions
  - `PublishedOf<T>()` — filter published events
  - `PublishedCount` — count events
- `FakeTenantContext` — configurable tenant for test scenarios
- `FakeCurrentUser` — wildcard permission matching (orders.* matches orders.create)

**Usage:**
```csharp
var fakeEventBus = new FakeEventBus();
serviceCollection.AddSingleton<IEventBus>(fakeEventBus);

// Later in test
var publishedEvents = fakeEventBus.PublishedOf<OrderPlacedIntegrationEvent>();
Assert.NotEmpty(publishedEvents);
```

**Dependencies:** Nac.Abstractions, Nac.Mediator

**LOC:** ~104

**Notes:** No Moq/NSubstitute needed for these types. Wildcard matching: `orders.create` matches both `orders.*` and `*.create` permissions.

---

## 14. Nac.Cli

**Purpose:** dotnet CLI tool (`nac` command) for scaffolding and management.

**Files:** 4 (commands, templates, DI, main)

**Commands:**
- `nac new <Name>` — scaffold solution
- `nac add module <Name>` — add module to existing solution
- `nac add feature <Module>/<Feature>` — generate Command, Handler, Validator, Endpoint
- `nac add entity <Module>/<Entity>` — generate Entity + Repository interface
- `nac add event <Module>/<Event>` — generate Domain Event + handler skeleton
- `nac add integration-event <Name>` — generate Integration Event in Shared Contracts
- `nac migration add <Module> "<Desc>"` — create EF migration
- `nac migration apply [Module]` — apply migrations
- `nac check architecture` — verify module boundaries
- `nac check health` — verify configurations

**Implementation:**
- `NewCommand`, `AddCommand` — main scaffolding logic
- `CodeTemplates` — embedded Scriban templates
- System.CommandLine for CLI parsing

**Templates Generated:**
- Module folder structure
- DbContext skeleton
- Feature (Command + Handler + Validator + Endpoint)
- Entity + Repository interface

**Dependencies:** System.CommandLine 2.0.5

**LOC:** ~408

**Notes:** Embedded templates as C# raw strings. placeholders: `{ModuleName}`, `{EntityName}`, `{Namespace}`. nac.json tracks module versions and dependencies.

---

## 15. Nac.Templates

**Purpose:** dotnet new templates for project initialization.

**Files:** 1 (template definition, nac.json, Program.cs skeleton)

**Template:** `nac-solution`
- Command: `dotnet new nac-solution --name MyApp`
- Generates folder structure + initial Program.cs with NacFrameworkBuilder

**Structure Generated:**
```
src/
  MyApp.Host/
  MyApp.Shared/
tests/
nac.json
MyApp.slnx
```

**Dependencies:** None (template content only)

**LOC:** ~16

**Notes:** Minimal template. Full scaffolding happens via `nac new` CLI command, not dotnet templates (which are simpler, template-only).

---

## Dependency Visualization

```
Nac.Abstractions [ZERO DEPS]
    ↑
    ├─ Nac.Domain
    ├─ Nac.Mediator
    └─ (all others depend directly or transitively)
            ↑
            ├─ Nac.Persistence ← Domain, Mediator
            │   ├─ Nac.Persistence.PostgreSQL
            │   └─ Nac.Messaging ← Persistence
            │       ├─ Nac.Messaging.RabbitMQ
            │       └─ (Outbox worker)
            │
            ├─ Nac.MultiTenancy
            ├─ Nac.Caching ← Mediator
            ├─ Nac.Observability ← Mediator
            ├─ Nac.WebApi
            └─ Nac.Testing ← Mediator

    [Distribution]
    ├─ Nac.Cli [System.CommandLine]
    └─ Nac.Templates [none]
```

---

## Statistics

| Metric | Value |
|--------|-------|
| **Total LOC** | ~4,575 |
| **Total files** | ~100 .cs |
| **Largest package** | Nac.Mediator (760 LOC) |
| **Smallest package** | Nac.Persistence.PostgreSQL (30 LOC) |
| **Target framework** | net10.0 |
| **Language** | C# 13 |
| **Package dependencies** | ~15 external (EF Core, RabbitMQ.Client, System.CommandLine, Npgsql) |

---

## Dependency Versions (v1.0)

**Framework:**
- .NET 10.0 (LTS)
- C# 13
- ASP.NET Core 10.0

**Core NuGet:**
- EF Core Relational 10.0.5
- RabbitMQ.Client 7.2.1
- System.CommandLine 2.0.5
- Npgsql (latest)

**Note:** All v1.0 packages versioned 1.0.0. Sync across NuGet feed required before release.

