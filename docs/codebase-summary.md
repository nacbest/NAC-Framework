# NAC Framework — Codebase Summary

Comprehensive package-by-package breakdown of the 15 NuGet packages in the NAC Framework.

---

## 1. Nac.Core

**Purpose:** Zero-dependency interfaces, markers, and domain base types.

**Key Types:**

**Base Types (moved from Nac.Domain):**
- `Entity<TId>` — base entity with Id, domain event collection, ID-based equality
- `AggregateRoot<TId>` — transactional boundary, optimistic concurrency (`Version: uint`)
- `ValueObject` — immutable, component-based equality (abstract)
- `StronglyTypedId` — ID generator pattern
- `Enumeration<TValue>` — strongly-typed enum base
- `IHasDomainEvents` — entity contract

**Contracts:**
- `INotification` — in-process pub/sub marker (Entity depends on it)
- `IEventBus`, `IIntegrationEvent`, `IIntegrationEventHandler` — messaging contracts
- `ITenantContext`, `TenantInfo` — multi-tenancy
- `ICurrentUser`, `IRequirePermission`, `IIdentityService`, `UserInfo` — auth contracts
- `ICacheable`, `ICacheInvalidator` — caching contracts
- `IAuditable`, `ITransactional` — behavioral markers
- `NacException` — base framework exception
- `IDateTimeProvider`, `SystemDateTimeProvider` — time abstraction
- `PaginationRequest` — pagination contracts
- `DomainError` — typed error value

**Dependencies:** Microsoft.Extensions.DependencyInjection.Abstractions only (no ASP.NET Core)

**Notes:** L0 contract layer. Zero ASP.NET Core dependency guarantee maintained strictly. ICommand/IQuery live in Nac.CQRS (L1), not here.

---

## 2. Nac.Domain

**Purpose:** Domain events and persistence contracts.

**Key Types:**

**Domain Events:**
- `DomainEvent` — record with `EventId`, `OccurredAt`, inherits from `INotification`
- `IAuditableEntity`, `ISoftDeletable` — entity contracts

**Persistence (Nac.Domain.Persistence namespace):**
- `IRepository<T>`, `IReadRepository<T>`, `IUnitOfWork` — persistence contracts
- `Specification<T>` — query specification pattern

**Dependencies:** Nac.Core only

**Notes:** Base entity types (`Entity`, `AggregateRoot`, `ValueObject`, etc.) now live in Nac.Core. This package provides DomainEvent + persistence interfaces. Handlers never call `SaveChanges` — UnitOfWork does.

---

## 3. Nac.CQRS

**Purpose:** Custom CQRS mediator with pipeline behaviors. (Renamed from Nac.Mediator; all namespaces `Nac.CQRS.*`)

**Key Types:**

**Abstractions (Nac.CQRS.Abstractions):**
- `ICommand<T>`, `ICommand` — CQRS command markers
- `IQuery<T>` — read-only query marker
- `ICommandHandler<T>`, `ICommandHandler<T, TResult>` — implement for commands
- `IQueryHandler<T, TResult>` — implement for queries
- `INotificationHandler<T>` — implement for domain events
- `ICommandBehavior<T>`, `IQueryBehavior<T>` — separate pipeline middlewares

**Core (Nac.CQRS.Core):**
- `IMediator` — interface: `Send(ICommand)`, `Send<T>(ICommand<T>)`, `Send<T>(IQuery<T>)`, `PublishAsync(INotification)`

**Internal:**
- `NacMediator` — orchestrator, delegates to wrappers
- `CommandWrapper`, `QueryWrapper`, `VoidCommandWrapper`, `NotificationWrapper` — type-erased dispatch
- `RequestDelegates`, `Unit` — pipeline delegates and void result

**Registration:**
- `HandlerRegistry` — fail-fast validation; 1 handler per command
- `HandlerScanner` — reflection-based discovery (assembly scanning)
- `ServiceCollectionExtensions` — DI registration

**Pipeline Order (default):**

Command: ExceptionHandling → Logging → Validation → Authorization → TenantEnrichment → UnitOfWork → Handler

Query: ExceptionHandling → Logging → Validation → Authorization → Caching → Handler

**Dependencies:** Nac.Core only

**Notes:** Handler registry built at startup; missing handler = fail-fast. Behaviors ordered explicitly, not auto-discovered.

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

**Dependencies:** EF Core Relational 10.0.5, Nac.Core, Nac.Domain, Nac.CQRS

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

**Dependencies:** Nac.Core, Nac.Persistence, Nac.CQRS, ASP.NET Core

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

**Dependencies:** RabbitMQ.Client 7.2.1, Nac.Messaging, Nac.Persistence, Nac.Core

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

**Dependencies:** Nac.Core, ASP.NET Core

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

**Dependencies:** Nac.Core, Nac.CQRS

**Notes:** Query must implement `ICacheable` to be cached. Command must implement `ICacheInvalidator` to invalidate related keys.

---

## 10. Nac.Identity

**Purpose:** ASP.NET Core Identity integration, JWT tokens, authorization, tenant-scoped roles and permissions.

**Key Types:**

**Core Entities:**
- `NacIdentityUser` — ASP.NET Identity user (unsealed); has `TenantId`, `UpdatedAt`, `CreatedBy`, `IsDeleted`
- `TenantMembership` — Links user to tenant with role assignment
- `TenantRole` — Role + permission set scoped to tenant
- `RefreshToken` — JWT refresh token, tenant-aware, Redis-backed option

**Generic DbContext:**
- `NacIdentityDbContext<TUser> where TUser : NacIdentityUser` — generic identity DbContext

**Services (all generic over TUser):**
- `JwtTokenService<TUser>` — Generate/validate JWT access tokens, refresh flow
- `IdentityService<TUser>` (IIdentityService from Nac.Core) — Query user info from business modules
- `IdentityEventPublisher<TUser>` — Publish identity lifecycle events
- `TenantAwareUserManager<TUser>` — UserManager scoped to current tenant (in MultiTenancy/)
- `TenantRoleService` — Manage tenant roles and user memberships
- `RefreshTokenStore` — Abstract token storage (EF Core or Redis)
- `RefreshTokenCleanupService` — BackgroundService, auto-expire tokens

**DI Registration:**
- `AddNacIdentity()` — default (uses NacIdentityUser)
- `AddNacIdentity<TUser>()` — generic overload for custom user types

**Behaviors:**
- `AuthorizationBehavior` — Check `IRequirePermission` at mediator level

**Integration Events:**
- `UserRegisteredIntegrationEvent`
- `UserEmailConfirmedIntegrationEvent`
- `PasswordResetIntegrationEvent`

**Middleware & Extensions:**
- `IdentityApplicationBuilderExtensions` — `UseNacIdentity()`, permission preload
- `IdentitySeeder` — Seed default roles per tenant

**Dependencies:** Nac.Core, Nac.CQRS, Nac.Persistence, Nac.MultiTenancy, ASP.NET Identity, JWT Bearer, StackExchange.Redis (optional)

**Notes:** Permissions loaded async at startup via middleware, safe to access synchronously in handlers. RefreshToken stores TenantId (preserved on refresh). Generic over TUser allows custom user types.

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

**Dependencies:** Nac.Core, Nac.CQRS

**Notes:** Uses `ILogger<T>`. Correlation ID propagates from HTTP request. Structured logging (ILogger.LogInformation) for easy machine parsing.

---

## 12. Nac.WebApi

**Purpose:** Response envelopes, global exception handler, module framework registration.

**Key Types:**

**Response Envelopes:**
- `ApiResponse<T>` — record: `Data: T`, `Meta: ResponseMeta`
- `PaginatedResponse<T>` — record: `Data: T[]`, `Pagination`, `Meta`
- `ErrorResponse` — record: `Error`, `Meta` (no data)

**Module Framework (Nac.WebApi.Modularity — moved from Nac.Abstractions):**
- `INacModule` — marker interface for module registration
- `NacFrameworkBuilder` — fluent builder for framework setup
- `NacServiceCollectionExtensions` — DI registration helpers

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

**Dependencies:** Nac.Core, ASP.NET Core

**LOC:** ~180

**Notes:** Envelope standardized across all endpoints. Correlation ID included in all error responses. Module framework enables fluent host setup.

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

**Dependencies:** Nac.Core, Nac.CQRS

**Notes:** No Moq/NSubstitute needed for these types. Wildcard matching: `orders.create` matches both `orders.*` and `*.create` permissions.

---

## 14. Nac.Cli

**Purpose:** dotnet global tool (`nac` command) for scaffolding full NAC Framework projects.

**Files:** 3 (Program.cs, Commands/NewCommand.cs, Services/ScaffoldService.cs) + embedded templates

**Commands:**
- `nac new <name> [--module <name>] [--output <dir>]` — scaffold a new NAC solution with host, shared contracts, module core, module infrastructure, and tests projects

**Implementation:**
- `NewCommand` — CLI parsing, input validation (C# identifier regex), delegates to `ScaffoldService`
- `ScaffoldService` — loads embedded Scriban templates, renders with `{project_name}`, `{module_name}`, `{nac_version}` model, writes 22 output files, runs `dotnet restore`

**Templates Generated (22 files):**
- Solution: `{Name}.slnx`, `nac.json`, `Directory.Build.props`, `Directory.Packages.props`
- Host: `{Name}.Host.csproj`, `Program.cs`, `appsettings.json`, `appsettings.Development.json`
- Shared: `{Name}.Shared.csproj`
- Module core: csproj, module class, entity, command + handler, query + handler, endpoints
- Module infrastructure: csproj, DbContext, entity configuration, infrastructure extensions
- Tests: test project csproj

**Dependencies:** System.CommandLine, Scriban

**Notes:** `PackAsTool=true`, `ToolCommandName=nac`. Templates embedded as resources (`.sbn` = Scriban, `.cstemplate` = verbatim C#). Template model keys use snake_case (`project_name`, `module_name`, `nac_version`). Placed under `/src/Tooling/` in solution.

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
L0: Nac.Core [DI.Abstractions only — no ASP.NET Core]
    ↑ (base types + contracts: Entity, AggregateRoot, ICurrentUser, INotification, etc.)
    │
L1: ├─ Nac.Domain     (DomainEvent, persistence contracts — Nac.Domain.Persistence)
    ├─ Nac.CQRS        (ICommand, IQuery, IMediator, pipeline behaviors)
    └─ Nac.Caching     (ICacheable behaviors)
            ↑
L2+:        ├─ Nac.Persistence ← Core, Domain, CQRS
            │   ├─ Nac.Persistence.PostgreSQL
            │   ├─ Nac.Identity ← Persistence, CQRS, MultiTenancy
            │   └─ Nac.Messaging ← Persistence
            │       └─ Nac.Messaging.RabbitMQ
            │
            ├─ Nac.MultiTenancy ← Core
            ├─ Nac.Observability ← Core, CQRS
            ├─ Nac.WebApi ← Core  (gains INacModule, NacFrameworkBuilder)
            └─ Nac.Testing ← Core, CQRS

L3 (Distribution):
    ├─ Nac.Cli [System.CommandLine]
    └─ Nac.Templates [none]
```

---

## Statistics

| Metric | Value |
|--------|-------|
| **Total LOC** | ~5,355 |
| **Total files** | ~130 .cs |
| **Largest package** | Nac.Identity (780 LOC) |
| **Smallest package** | Nac.Persistence.PostgreSQL (30 LOC) |
| **Target framework** | net10.0 |
| **Language** | C# 13 |
| **Package dependencies** | ~15 external (EF Core, RabbitMQ.Client, System.CommandLine, Npgsql, JWT) |

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

