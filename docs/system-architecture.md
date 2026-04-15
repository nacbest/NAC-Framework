# NAC Framework — System Architecture

Comprehensive overview of NAC Framework's architectural patterns, data flows, and design decisions.

---

## Architecture Overview

NAC Framework combines **Clean Architecture** (layered, dependency inversion) with **Vertical Slice Modularity** (feature-driven, autonomous modules).

### Why Not Pure Clean Architecture?

Pure Clean Architecture (separate projects per layer: Domain, Application, Infrastructure) creates friction at scale:

- Single business feature requires changes across 3-4 projects
- Cross-module contracts muddy responsibility boundaries
- Dependency graphs become fragile and hard to navigate

### NAC Approach: Module = Unit of Deployment

Each module is self-contained with its own Domain/Application/Infrastructure layers. Modules communicate via:
- **Integration Events** (async, eventual consistency)
- **Module Contracts** (sync queries, interfaces only)
- **Shared Kernel** (minimal, stable types)

**Result:** Monolith today, microservices tomorrow without rearchitecture.

---

## CQRS Pipeline Architecture

### Message Types

NAC enforces **strict CQRS separation** via distinct message types:

| Type | Purpose | Mutates | Transactions | Caching | Auditing |
|------|---------|---------|--------------|---------|----------|
| **Command** | Write operation | Yes | ✓ | ✗ | ✓ |
| **Query** | Read operation | No | ✗ | ✓ | ✗ |
| **Notification** | In-process event | No | ✗ | ✗ | ✗ |

**Why separate?** If a single interface existed, behaviors could apply to both—breaking CQRS separation.

### Command Pipeline

```
HTTP Request
  ↓
Endpoint Receives Command
  ↓
Mediator.Send(command)
  ↓
[Pipeline Behaviors (in order)]
  ├─ 1. ExceptionHandling — catch + log + rethrow
  ├─ 2. Logging — entry + parameters
  ├─ 3. Validation — FluentValidation
  ├─ 4. Authorization — IRequirePermission check
  ├─ 5. TenantEnrichment — set tenant context (if multitenancy)
  ├─ 6. UnitOfWork — open transaction
  │   ├─ 7. Handler — business logic (no SaveChanges!)
  │   ├─ 8. SaveChanges — EF Core commit
  │   └─ 9. Domain Event Dispatch — publish in-process notifications
  └─ 10. (response)
  ↓
HTTP Response (200, 4xx, 5xx)
```

**Key Points:**
- Handler NEVER calls SaveChanges—UnitOfWork behavior does
- Domain events dispatch AFTER transaction commits (post-commit pattern)
- Behaviors are registered explicitly in order; no auto-discovery

### Query Pipeline

```
HTTP Request
  ↓
Endpoint Receives Query
  ↓
Mediator.Send(query)
  ↓
[Pipeline Behaviors (in order)]
  ├─ 1. ExceptionHandling
  ├─ 2. Logging — entry + parameters
  ├─ 3. Validation
  ├─ 4. Authorization — IRequirePermission check
  ├─ 5. Caching — check cache (if ICacheable)
  │   ├─ Hit: return cached result
  │   └─ Miss: continue to handler
  ├─ 6. Handler — fetch data
  └─ 7. CachingStoreResult — store result if ICacheable
  ↓
HTTP Response (200, 404, 5xx)
```

**Lighter than commands:** No transaction, no domain events, just read + optional cache.

---

## Dual Event System

NAC supports TWO event buses for different purposes:

### Layer 1: Domain Events (In-Process)

**Scope:** Within a single module, immediate consistency

**Flow:**
```
1. Handler processes command
2. Business logic mutates aggregate
3. Aggregate raises domain event (added to collection)
4. UnitOfWork commits transaction
5. Domain events collected from tracked entities
6. Mediator.PublishAsync(notification) for each event
7. Event handlers run in-process, same request scope
```

**Characteristics:**
- Fire-and-forget within same request
- No serialization needed
- Perfect for internal module consistency
- If handler needs to write → new transaction

**Example:**
```csharp
public sealed class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateOrderCommand cmd, CancellationToken ct)
    {
        var order = Order.Create(cmd.CustomerId, cmd.Items);  // Raises OrderCreatedDomainEvent
        _repository.Add(order);
        // Don't call SaveChanges! UnitOfWork behavior does it post-handler
        
        // Domain event handled by OrderCreatedDomainEventHandler
        // (which may publish integration event)
        return order.Id;
    }
}
```

### Layer 2: Integration Events (Distributed)

**Scope:** Between modules, eventual consistency

**Flow:**
```
1. Domain event handler publishes integration event to IEventBus
2. Bus implementation decides:
   
   InMemoryEventBus:
   ├─ Store in Channel
   └─ Worker picks up async, invokes handlers
   
   OutboxEventBus:
   ├─ Persist to OutboxMessage table
   ├─ Background worker polls Outbox
   └─ Publishes to actual broker (RabbitMQ)
   
   RabbitMqEventBus:
   ├─ Serialize to JSON
   ├─ Publish to topic exchange
   └─ Subscribers consume (with idempotency via event ID)
```

**Characteristics:**
- Async, bounded contexts
- Requires serialization
- Broker-based (if using RabbitMQ/Kafka)
- Consumer responsible for idempotency
- At-least-once delivery guarantee (Outbox pattern)

**Example:**
```csharp
public sealed class OrderCreatedDomainEventHandler : INotificationHandler<OrderCreatedDomainEvent>
{
    public async Task HandleAsync(OrderCreatedDomainEvent evt, CancellationToken ct)
    {
        // Publish integration event (async, may fail and retry)
        await _eventBus.PublishAsync(new OrderCreatedIntegrationEvent(
            evt.OrderId,
            evt.CustomerId,
            evt.TotalAmount
        ), ct);
    }
}

// Subscriber in different module (e.g., Inventory)
public sealed class OrderCreatedIntegrationEventHandler 
    : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    public async Task HandleAsync(OrderCreatedIntegrationEvent evt, CancellationToken ct)
    {
        // Consume and process (maybe deduct inventory)
    }
}
```

### Event Bus Abstraction

**Single interface, swappable implementation:**

```csharp
public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) 
        where T : IIntegrationEvent;
}

// Development: InMemoryEventBus (fast, single-process)
services.AddInMemoryEventBus();

// Production: RabbitMQ (distributed, reliable)
services.AddRabbitMqEventBus(opts =>
{
    opts.HostName = "rabbitmq";
    opts.ExchangeName = "nac.events";
});
```

---

## Persistence Architecture

### DbContext Per Module (Mandatory)

Each module has 2 projects: **core** (persistence-ignorant) and **infrastructure** (EF Core).
The DbContext lives in the `.Infrastructure` project, not in module core.

```
{Ns}.Modules.Catalog/                          ← Core (clean)
  Domain/Entities/Product.cs
  Application/Commands/...
  Contracts/IProductRepository.cs
  Endpoints/...

{Ns}.Modules.Catalog.Infrastructure/           ← Infrastructure (EF Core)
  CatalogDbContext.cs
  CatalogInfrastructureExtensions.cs
  Configurations/ProductConfiguration.cs
  Repositories/ProductRepository.cs
```

**DbContext** (in `.Infrastructure`):

```csharp
// In {Ns}.Modules.Catalog.Infrastructure/CatalogDbContext.cs
public sealed class CatalogDbContext : NacDbContext
{
    public CatalogDbContext(
        DbContextOptions<CatalogDbContext> options,
        ICurrentUser? currentUser = null) : base(options, currentUser) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
```

**DI extension** (in `.Infrastructure`):

```csharp
// In {Ns}.Modules.Catalog.Infrastructure/CatalogInfrastructureExtensions.cs
public static class CatalogInfrastructureExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddNacPostgreSQL<CatalogDbContext>(connectionString);
        services.AddNacRepositoriesFromAssembly<CatalogDbContext>(
            typeof(CatalogModule).Assembly);
        return services;
    }
}

// Host Program.cs — 1 line per module
services.AddCatalogInfrastructure(connectionString);
```

**Benefits:**
- Module core stays persistence-ignorant (no EF Core references)
- Clear module boundaries with separate projects
- Independent migrations per module
- Multi-tenancy isolation at DbContext level
- Module team owns both projects independently
- Ready for microservice extraction

### Unit of Work Pattern

**UnitOfWork behavior wraps handler in transaction:**

```csharp
// Handler never opens transaction
public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateProductCommand cmd, CancellationToken ct)
    {
        // UnitOfWorkBehavior already opened transaction
        var product = new Product { Name = cmd.Name };
        _repository.Add(product);
        // Don't call SaveChanges! UnitOfWork will do it
        return product.Id;
    }
}

// UnitOfWorkBehavior execution
public async Task<Unit> Handle(
    ICommand request,
    CommandHandlerDelegate next,
    CancellationToken ct)
{
    using (var transaction = await _context.Database.BeginTransactionAsync(ct))
    {
        try
        {
            await next(request, ct);  // Handler runs in transaction
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            
            // AFTER commit: dispatch domain events
            await DispatchDomainEvents(ct);
            return Unit.Value;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
```

### Repository Pattern (No IQueryable)

**Repositories never expose IQueryable.** Queries use Specification pattern:

```csharp
// Specification encapsulates query logic
public sealed class GetProductsByPriceRangeSpec : Specification<Product>
{
    public GetProductsByPriceRangeSpec(decimal minPrice, decimal maxPrice)
    {
        Query
            .Where(p => p.Price >= minPrice && p.Price <= maxPrice)
            .OrderBy(p => p.Price)
            .Take(100);
    }
}

// Repository returns complete result, never IQueryable
public sealed class ProductRepository : EfRepository<Product>, IProductRepository
{
    public async Task<IEnumerable<Product>> GetByPriceRangeAsync(
        decimal minPrice,
        decimal maxPrice,
        CancellationToken ct)
    {
        var spec = new GetProductsByPriceRangeSpec(minPrice, maxPrice);
        return await GetAsync(spec, ct);  // Returns List<Product>
    }
}

// ✗ This doesn't exist:
public IQueryable<Product> GetAll();  // Never!
```

---

## Multi-Tenancy Architecture

### Resolution Pipeline

HTTP request → **TenantResolutionMiddleware** → Try resolvers in chain order:

```
1. Header ("X-Tenant-ID" or custom header)
   ├─ Hit: Extract ID
   └─ Miss: Try next

2. Claim (from JWT token: "tenant_id" claim)
   ├─ Hit: Extract ID
   └─ Miss: Try next

3. Subdomain (abc.myapp.com → "abc")
   ├─ Hit: Extract ID
   └─ Miss: Try next

4. Route Parameter (/api/tenants/{id}/products → id)
   ├─ Hit: Extract ID
   └─ Miss: Try next

5. Query String (?tenant_id=abc)
   ├─ Hit: Extract ID
   └─ Miss: Default

Result: Set ITenantContext scoped to request, fail if unresolvable
```

**Configuration:**
```csharp
builder.AddNacFramework(nac =>
{
    nac.UseMultiTenancy(tenant =>
    {
        tenant.Strategy = TenantStrategy.PerSchema;  // or Discriminator, DatabasePerTenant
        tenant.ResolutionChain = new[] 
        { 
            TenantResolution.Header, 
            TenantResolution.Claim, 
            TenantResolution.Subdomain 
        };
    });
});
```

### Data Isolation Strategies

#### Strategy 1: Discriminator (Column)

**Every table has `TenantId` column, EF global filter hides others:**

```csharp
public sealed class Product : AggregateRoot<Guid>
{
    public Guid TenantId { get; set; }  // Discriminator
    public required string Name { get; init; }
}

// EF Configuration
builder.Entity<Product>()
    .HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);

// Result: All queries automatically include WHERE TenantId = @current
```

**Pros:** Single database, simple
**Cons:** Weak isolation, noisy schema

#### Strategy 2: Schema-per-Tenant

**Each tenant gets schema, DbContext switches schema at runtime:**

```csharp
public sealed class CatalogDbContext : NacDbContext
{
    public CatalogDbContext(
        DbContextOptions<CatalogDbContext> options,
        ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var schema = _tenantContext.IsMultiTenant 
            ? $"tenant_{_tenantContext.TenantId}" 
            : "public";
        optionsBuilder.UseNpgsql("...", opts => opts.UseLogicalNaming(schema));
    }
}

// Result: All queries hit tenant_abc.Products, tenant_xyz.Products, etc.
```

**Pros:** Better isolation, single DB
**Cons:** Migration complexity, schema management

#### Strategy 3: Database-per-Tenant

**Each tenant gets separate database, connection string resolved at runtime:**

```csharp
public sealed class TenantConnectionResolver : ITenantConnectionResolver
{
    public async Task<string> ResolveAsync(Guid tenantId)
    {
        var tenant = await _tenantStore.GetAsync(tenantId);
        return tenant.ConnectionString;  // "Host=db; Database=tenant_abc"
    }
}

// DbContext registration
services.AddDbContext<CatalogDbContext>((sp, opts) =>
{
    var tenantContext = sp.GetRequiredService<ITenantContext>();
    var connStr = await _resolver.ResolveAsync(tenantContext.TenantId);
    opts.UseNpgsql(connStr);
});

// Result: abc.Postgres, xyz.Postgres, each completely isolated
```

**Pros:** Maximum isolation, GDPR-friendly
**Cons:** Operational complexity, higher cost

### When Multitenancy Disabled

**Zero overhead:**

```csharp
// ITenantContext still exists
public interface ITenantContext
{
    Guid TenantId { get; }
    bool IsMultiTenant { get; }  // false
    string? Name { get; }
}

// Global query filter not registered
// No resolution middleware
// Queries run without TenantId filter
```

---

## Identity Architecture

### Multi-Layer Identity System

NAC Identity (`Nac.Identity`) provides **ASP.NET Identity + JWT + tenant-scoped roles**.

**Components:**
- **NacIdentityUser:** Global user account (unsealed; email, username, password; has TenantId, IsDeleted)
- **TenantMembership:** Links user to tenant with role assignment
- **TenantRole:** Role + permissions scoped to tenant
- **JwtCurrentUser\<TUser\>:** JWT-based `ICurrentUser` implementation with async permission loading
- **TenantAwareUserManager\<TUser\>:** UserManager scoped to current tenant
- **IdentityEventPublisher\<TUser\>:** Publishes identity lifecycle events
- **IIdentityService:** Query interface for business modules to fetch user info (from Nac.Core)

**Key Pattern: Async Permission Loading**

Permissions are loaded asynchronously via middleware to avoid sync-over-async penalties:

```csharp
// In IdentityApplicationBuilderExtensions.cs
app.UseNacIdentity();  // Preloads permissions before handlers run

// In JwtCurrentUser.cs
internal async Task LoadPermissionsAsync(CancellationToken ct = default)
{
    // Loads membership → role → permissions from DB
    // Called from middleware before request reaches handlers
}

// In handlers, ICurrentUser.Permissions is already cached synchronously
public async Task<Guid> Handle(CreateProductCommand cmd, CancellationToken ct)
{
    if (!_currentUser.HasPermission("products.create"))
        throw new ForbiddenException(...);
    // ...
}
```

**Identity Events (Integration)**

When `Nac.Messaging` is configured, `IdentityEventPublisher` publishes events:

```csharp
// In identity workflows (registration, confirmation, reset)
public sealed class UserRegisteredEvent(Guid UserId, string Email, string? TenantId) 
    : IIntegrationEvent;

public sealed class UserEmailConfirmedEvent(Guid UserId, string? TenantId) 
    : IIntegrationEvent;

public sealed class PasswordResetEvent(Guid UserId, string? TenantId) 
    : IIntegrationEvent;

// Usage in handlers
var publisher = new IdentityEventPublisher(_eventBus);
await publisher.PublishUserRegisteredAsync(newUser, tenantId, ct);
```

**RefreshToken Persistence**

Refresh tokens store `TenantId` at issuance time and preserve it on token rotation:

```csharp
public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string? TenantId { get; set; }  // Preserved on refresh
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTimeOffset.UtcNow;
}
```

---

## Authorization Architecture

### Permission-Based (Not Role-Based)

**Hierarchy:** `module.resource.action`

```csharp
// Examples:
"catalog.products.create"
"catalog.products.read"
"catalog.categories.manage"  // Wildcard: all actions on categories
"inventory.*"                 // Wildcard: all resources in inventory
"*.approve"                   // Wildcard: approve action anywhere

// Permission check
ICurrentUser.HasPermission("orders.create");  // Exact match
ICurrentUser.HasPermission("orders.*");       // Match anything orders.*
ICurrentUser.HasPermission("*.create");       // Match anything *.create
```

**Why?** Roles = permission sets, configurable at runtime. Flexible and audit-friendly.

### Authorization Behavior

**Marker interface triggers behavior:**

```csharp
public sealed record CreateProductCommand(...) 
    : ICommand<Guid>,
      IRequirePermission
{
    public string Permission => "catalog.products.create";
}

// AuthorizationCommandBehavior checks before handler
public async Task<Unit> HandleAsync(
    ICommand request,
    CommandHandlerDelegate next,
    CancellationToken ct)
{
    if (request is IRequirePermission perms)
    {
        if (!_currentUser.HasPermission(perms.Permission))
            throw new ForbiddenException($"Missing: {perms.Permission}");
    }
    
    return await next(request, ct);
}
```

### Tenant-Scoped Permissions

When multitenancy enabled, permissions are tenant-scoped:

```csharp
// User "alice" has "orders.create" in Tenant A only
_currentUser.HasPermission("orders.create");  // Only true in Tenant A context

// Different tenant context = different permissions
_tenantContext.TenantId = new Guid("tenant-b");
_currentUser.HasPermission("orders.create");  // false (alice has no permission in B)
```

---

## Module Communication

Module core defines contracts (interfaces in `Contracts/`), Module `.Infrastructure` implements them.

### 3 Patterns (by preference)

#### 1. Integration Events (PREFERRED)

**Async, eventual consistency, loosest coupling:**

```
Module A publishes OrderCreatedIntegrationEvent
    ↓
Outbox table stores event
    ↓
Background worker publishes to broker
    ↓
Module B subscribed to OrderCreatedIntegrationEvent
    ↓
Handler processes independently
```

**When to use:** Whenever possible. Most flexible.

#### 2. Module Contracts (Synchronous Query)

**When immediate data needed (Orders asking Catalog for price):**

```csharp
// Catalog exposes contract interface (in Shared Contracts)
namespace Shared.Contracts.Catalog
{
    public interface ICatalogModuleApi
    {
        Task<ProductPriceDto?> GetProductPriceAsync(Guid productId, CancellationToken ct);
    }
}

// Catalog implementation in its module
public sealed class CatalogModuleApi : ICatalogModuleApi
{
    public async Task<ProductPriceDto?> GetProductPriceAsync(Guid productId, CancellationToken ct)
    {
        var product = await _repository.GetByIdAsync(productId, ct);
        return product == null ? null : new ProductPriceDto(product.Id, product.Price);
    }
}

// Orders uses it
public sealed class CalculateOrderTotalQueryHandler : IQueryHandler<CalculateOrderTotalQuery, decimal>
{
    private readonly ICatalogModuleApi _catalogApi;  // Injected interface
    
    public async Task<decimal> Handle(CalculateOrderTotalQuery query, CancellationToken ct)
    {
        var total = 0m;
        foreach (var line in query.OrderLines)
        {
            var price = await _catalogApi.GetProductPriceAsync(line.ProductId, ct);
            total += price.Value * line.Quantity;
        }
        return total;
    }
}
```

**When to use:** Sync data requirements, tightly coupled concepts. Prefer over queries across modules.

#### 3. Shared Kernel

**Minimal, stable types (Money, Address, enums):**

```csharp
// Shared/Domain/ValueObjects/Money.cs
namespace Shared.Domain.ValueObjects
{
    public sealed class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }
        // ...
    }
}

// Every module can use Money without coupling
namespace Catalog.Modules.Domain;
public sealed class Product : AggregateRoot<Guid>
{
    public Money Price { get; init; }  // Shared type
}
```

**When to use:** Only for domain concepts used by 3+ modules and unlikely to change.

### Forbidden Patterns

**None of these:**
- Module A references Modules.Catalog project directly
- Module A queries Module B's DbContext
- Module A calls internal service in Module B
- Circular dependencies between modules

**Enforced by:** Code review and package dependency rules (see CLAUDE.md). `nac check architecture` is planned but not yet implemented.

---

## Caching Architecture

### Query-Level Caching

**Marker interface enables caching behavior:**

```csharp
public sealed record GetProductByIdQuery(Guid Id) 
    : IQuery<ProductDto>,
      ICacheable
{
    public string CacheKey => $"product:{Id}";
    public TimeSpan? Expiry => TimeSpan.FromMinutes(5);
}

// CachingQueryBehavior checks cache before handler
public async Task<ProductDto?> Handle(GetProductByIdQuery query, CancellationToken ct)
{
    // Behavior checks IDistributedCache first
    // If hit: return cached ProductDto
    // If miss: run handler, store result
}
```

### Cache Invalidation

**Command-level cache invalidation:**

```csharp
public sealed record UpdateProductCommand(Guid Id, string Name) 
    : ICommand,
      ICacheInvalidator
{
    public IEnumerable<string> GetInvalidationKeys()
    {
        yield return $"product:{Id}";
        yield return "products:list";
        yield return "products:search:*";  // Pattern
    }
}

// CacheInvalidationBehavior runs post-command
// Calls IDistributedCache.RemoveAsync for each key
```

### Cache Provider Abstraction

**Uses ASP.NET Core's IDistributedCache—swap provider via DI:**

```csharp
// Development: In-memory
services.AddDistributedMemoryCache();

// Production: Redis
services.AddStackExchangeRedisCache(opts => 
{
    opts.Configuration = "redis:6379";
});
```

---

## Observability Architecture

### Structured Logging

**Behaviors log entry/exit/duration/errors with correlation ID:**

```
INFO | CatalogModule | GetProductByIdQueryHandler
      Request={"Id":"abc123"} | Duration=15ms | CorrelationId=xyz789

ERROR | OrderModule | CreateOrderCommandHandler
      Exception=InsufficientInventoryException | Message=...
      CorrelationId=xyz789
```

**Uses ILogger<T> structured logging:**
```csharp
_logger.LogInformation(
    "Query {QueryName} completed in {Duration}ms",
    nameof(GetProductByIdQuery),
    stopwatch.ElapsedMilliseconds);
```

**Correlation ID:**
- Generated per request (HttpContextAccessor)
- Propagates to logs
- Included in error responses for tracing

### Metrics & Tracing (Future)

- OpenTelemetry-ready
- Per-module command/query counters
- Request duration histograms
- Distributed tracing (via correlation ID)

---

## Deployment Architecture

### Modular Monolith

**Single deployment unit, but modules are independent:**

```
Host Process
  ├─ Nac.Framework (shared)
  ├─ Modules.Catalog (independent DbContext, endpoints)
  ├─ Modules.Orders (independent DbContext, endpoints)
  └─ Modules.Inventory (independent DbContext, endpoints)
```

### Scaling Path

```
PHASE 1: Modular Monolith
  └─ All modules in single process
     
PHASE 2: Async Messaging
  ├─ Add RabbitMQ
  ├─ Replace InMemoryEventBus with OutboxEventBus
  └─ Background workers publish to broker
     
PHASE 3: Microservices
  ├─ Extract Module.Orders to Orders.Service
  ├─ Keep same IEventBus abstraction
  ├─ Modules communicate via broker
  └─ Zero architectural change (just config + deployment)
```

Each module's boundary is already clear—extraction is **mechanical, not architectural**.

---

## API Layer

### Minimal APIs (Not Controllers)

**Endpoints grouped by feature/module:**

```csharp
public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/catalog/products")
            .WithName("Products")
            .WithOpenApi();
        
        group.MapPost("/", CreateProduct)
            .WithName("CreateProduct")
            .Produces<ApiResponse<Guid>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);
        
        group.MapGet("/{id}", GetProductById)
            .WithName("GetProductById")
            .Produces<ApiResponse<ProductDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
    
    private static async Task<IResult> CreateProduct(
        CreateProductRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new CreateProductCommand(request.Name, request.Price);
        var productId = await mediator.Send(command, ct);
        return Results.Created($"/api/catalog/products/{productId}", 
            new ApiResponse<Guid>(productId));
    }
}
```

**Benefits:**
- Explicit routing
- Grouped by concern
- No controller bloat
- Clear handler → endpoint mapping

---

## Security & Error Handling

### Exception → HTTP Status Mapping

| Exception | Status | Body |
|-----------|--------|------|
| ValidationException | 400 | ErrorResponse with violations |
| UnauthorizedException | 401 | ErrorResponse (no details) |
| ForbiddenException | 403 | ErrorResponse (no details) |
| NotFoundException | 404 | ErrorResponse |
| ConflictException | 409 | ErrorResponse |
| DomainException | 422 | ErrorResponse with details |
| Unhandled | 500 | ErrorResponse + CorrelationId |

### Response Envelope

**Consistent across all endpoints:**

```json
// Success (200)
{
  "data": {
    "id": "abc123",
    "name": "Laptop"
  },
  "meta": {
    "timestamp": "2026-04-12T14:30:00Z",
    "traceId": "xyz789"
  }
}

// Error (400+)
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "Validation failed",
    "details": [
      { "field": "Name", "message": "Required" }
    ]
  },
  "meta": {
    "timestamp": "2026-04-12T14:30:00Z",
    "traceId": "xyz789"
  }
}

// Paged (200)
{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "total": 100
  },
  "meta": {...}
}
```

---

## Summary Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     HTTP Request                            │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
          ┌──────────────────────────────┐
          │  TenantResolutionMiddleware  │
          │  (Set ITenantContext)        │
          └────────────┬─────────────────┘
                       │
                       ▼
          ┌──────────────────────────────┐
          │   Endpoint (Minimal API)     │
          │   Mediator.Send(command)     │
          └────────────┬─────────────────┘
                       │
        ┌──────────────┴──────────────┐
        │                             │
        ▼                             ▼
   [COMMAND]                     [QUERY]
        │                             │
        ├─Exception Handling          ├─Exception Handling
        ├─Logging                     ├─Logging
        ├─Validation                  ├─Validation
        ├─Authorization               ├─Authorization
        ├─Tenant Enrichment           ├─Caching (check)
        ├─UnitOfWork {                │
        │  ├─Transaction {            │
        │  │  ├─Handler               ├─Handler
        │  │  └─SaveChanges           │
        │  │}                          ├─Caching (store)
        │  └─Domain Events            │
        │}                             │
        │                             │
        └──────────────┬──────────────┘
                       │
                       ▼
          ┌──────────────────────────────┐
          │    Response (ApiResponse)    │
          │    + CorrelationId + Status  │
          └────────────┬─────────────────┘
                       │
                       ▼
                  HTTP Response
```

