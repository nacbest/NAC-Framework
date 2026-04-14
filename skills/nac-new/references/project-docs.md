# Project Documentation Templates

Replace `{Name}` with solution name.

## CLAUDE.md

```markdown
# {Name} — NAC Framework Project

## Framework

Built on **NAC Framework v1.0** — modular .NET 10 foundation with CQRS, multi-tenancy, and clean architecture.

## Architecture Rules

### Package Dependencies (CRITICAL)

```
┌───────────────────────────────────────────────────────────────┐
│                         Host Layer                             │
│   References ALL packages, wires DI, runs app                  │
│   Can reference: Nac.Identity, Nac.Persistence.PostgreSQL, etc │
└─────────────────────────────┬─────────────────────────────────┘
                              │
┌─────────────────────────────┴─────────────────────────────────┐
│                    Infrastructure Packages                     │
│   Nac.Identity, Nac.Persistence.PostgreSQL, Nac.Caching        │
│   (NEVER referenced by Business Modules)                       │
└─────────────────────────────┬─────────────────────────────────┘
                              │
┌─────────────────────────────┴─────────────────────────────────┐
│                    Business Modules Layer                      │
│   {Name}.Modules.Staff, {Name}.Modules.Customer, etc.          │
│   Can ONLY reference: Nac.Abstractions, Nac.Domain, Nac.Mediator│
└─────────────────────────────┬─────────────────────────────────┘
                              │
┌─────────────────────────────┴─────────────────────────────────┐
│                    Nac.Abstractions Layer                      │
│   ICurrentUser, IRepository, ICommand, ITenantContext          │
│   (Contracts only - NO implementation)                         │
└───────────────────────────────────────────────────────────────┘
```

### Layer Boundaries

```
Domain (innermost)
  ↑ NO external dependencies except Nac.Domain
  │
Application
  ↑ Depends on: Domain, Nac.Abstractions, Nac.Mediator
  │
Infrastructure
  ↑ Implements interfaces from Application
  ↑ Depends on: Application, Domain, EF Core, external libs
  │
Endpoints (outermost)
  ↑ Only calls Application via IMediator
  ↑ NO direct Domain/Infrastructure access
```

### Identity Linking Pattern

Business modules link to NacUser via **Guid primitive only**:

```csharp
// ✅ CORRECT - Guid primitive, no navigation property
public sealed class Staff : AggregateRoot<Guid>
{
    public Guid UserId { get; set; }  // FK to NacUser.Id
    // ... business fields
}

// ❌ WRONG - Navigation property couples to Infrastructure
public sealed class Staff : AggregateRoot<Guid>
{
    public NacUser User { get; set; }  // FORBIDDEN!
}
```

Access user info via `ICurrentUser` from Nac.Abstractions:

```csharp
public class GetStaffHandler : IQueryHandler<GetStaffQuery, StaffDto?>
{
    private readonly ICurrentUser _currentUser;

    public async Task<StaffDto?> Handle(GetStaffQuery query, CancellationToken ct)
    {
        var userId = Guid.Parse(_currentUser.UserId!);
        return await _staffRepo.GetByUserIdAsync(userId, ct);
    }
}
```

### Forbidden Patterns

- **Business modules referencing Nac.Identity** — use ICurrentUser
- **Navigation properties to NacUser** — use Guid UserId only
- **Cross-module DbContext access** — modules are isolated
- **Direct project references between modules** — use Integration Events
- **IQueryable exposure from repositories** — return concrete types
- **Handlers calling SaveChanges** — UnitOfWork behavior handles it

### File Placement Rules

| Type | Location | Example |
|------|----------|---------|
| Entity/Aggregate | `Domain/Entities/` | `Product.cs` |
| Value Object | `Domain/Entities/` | `Money.cs` |
| Domain Event | `Domain/Events/` | `ProductCreatedDomainEvent.cs` |
| Specification | `Domain/Specifications/` | `ActiveProductsSpec.cs` |
| Command | `Application/Commands/` | `CreateProductCommand.cs` |
| Command Handler | `Application/Commands/` | `CreateProductHandler.cs` |
| Query | `Application/Queries/` | `GetProductQuery.cs` |
| Query Handler | `Application/Queries/` | `GetProductHandler.cs` |
| Domain Event Handler | `Application/EventHandlers/` | `ProductCreatedHandler.cs` |
| DbContext | `Infrastructure/Persistence/` | `CatalogDbContext.cs` |
| EF Configuration | `Infrastructure/Persistence/` | `ProductConfiguration.cs` |
| Repository | `Infrastructure/Repositories/` | `ProductRepository.cs` |
| Endpoint | `Endpoints/` | `ProductEndpoints.cs` |

## Quick Reference

### Code Generation
```bash
/nac-add-module <Name>              # New module
/nac-add-feature <Module>/<Name>    # Command + Handler + Endpoint
/nac-add-entity <Module>/<Name>     # Entity in Domain/Entities/
```

### CQRS Pattern
- **Commands** (write): `ICommand<T>` with marker interfaces
- **Queries** (read): `IQuery<T>` with optional caching
- **Handlers never call SaveChanges** — UnitOfWork handles it

### Marker Interfaces
- `ITransactional` — wrap in DB transaction
- `IRequirePermission` — check `Permission` property
- `ICacheable` — cache query result
- `ICacheInvalidator` — invalidate cache keys post-command
- `IAuditable` — log audit trail

## Module Structure

```
Modules/{Name}.Modules.{Module}/
├── Domain/
│   ├── Entities/           — entities, aggregates, value objects
│   ├── Events/             — domain events
│   └── Specifications/     — query specifications
├── Application/
│   ├── Commands/           — commands + handlers
│   ├── Queries/            — queries + handlers
│   └── EventHandlers/      — domain event handlers
├── Infrastructure/
│   ├── Persistence/        — DbContext, EF configurations
│   └── Repositories/       — repository implementations
├── Endpoints/              — minimal API endpoints
└── {Module}Module.cs       — module registration
```

## Testing
Use Fakes from `Nac.Testing`: `FakeEventBus`, `FakeTenantContext`, `FakeCurrentUser`

## Documentation
See `llms.txt` for detailed NAC Framework patterns.
```

## llms.txt

```markdown
# {Name}

> .NET 10 backend API built on NAC Framework v1.0 with CQRS, modular architecture, and clean domain-driven design.

## Project Structure

- CLAUDE.md: AI assistant instructions and code patterns
- nac.json: Framework configuration and module registry
- src/{Name}.Host/: Composition root, Program.cs, DI setup
- src/Modules/: Feature modules (Domain, Application, Infrastructure, Endpoints)
- tests/: Unit and integration tests

## Skills

```bash
/nac-add-module <Name>              # New module
/nac-add-feature <Module>/<Name>    # Command + Handler + Endpoint
/nac-add-entity <Module>/<Name>     # Entity + Repository
/nac-install-identity               # Add authentication
```

## CQRS Pattern

### Commands (Write Operations)

```csharp
// Command with marker interfaces
public sealed record CreateProductCommand(string Name, decimal Price)
    : ICommand<Guid>,
      ITransactional,
      IRequirePermission,
      IAuditable
{
    public string Permission => "catalog.products.create";
}

// Handler - NEVER call SaveChanges
public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        var product = Product.Create(cmd.Name, cmd.Price);
        _repository.Add(product);
        return product.Id;  // UnitOfWork commits after handler
    }
}
```

### Queries (Read Operations)

```csharp
public sealed record GetProductByIdQuery(Guid Id)
    : IQuery<ProductDto>,
      ICacheable
{
    public string CacheKey => $"product:{Id}";
    public TimeSpan? Expiry => TimeSpan.FromMinutes(5);
}
```

## Marker Interfaces

| Interface | Purpose |
|-----------|---------|
| ITransactional | Wrap handler in DB transaction |
| IRequirePermission | Check Permission property |
| ICacheable | Cache query result |
| ICacheInvalidator | Invalidate cache keys |
| IAuditable | Log audit trail |

## Domain Events

```csharp
public sealed record ProductCreatedDomainEvent(Guid ProductId) : DomainEvent;

public sealed class Product : AggregateRoot<Guid>
{
    public static Product Create(string name, decimal price)
    {
        var product = new Product { Id = Guid.NewGuid(), Name = name, Price = price };
        product.RaiseDomainEvent(new ProductCreatedDomainEvent(product.Id));
        return product;
    }
}

public sealed class ProductCreatedHandler : INotificationHandler<ProductCreatedDomainEvent>
{
    public async Task Handle(ProductCreatedDomainEvent evt, CancellationToken ct)
    {
        await _eventBus.PublishAsync(new ProductCreatedIntegrationEvent(evt.ProductId), ct);
    }
}
```

## Module Structure

```
Modules/{Module}/
├── Domain/
│   ├── Entities/{Entity}.cs
│   ├── Events/{Event}DomainEvent.cs
│   └── Specifications/{Spec}Spec.cs
├── Application/
│   ├── Commands/{Command}Command.cs, {Command}Handler.cs
│   ├── Queries/{Query}Query.cs, {Query}Handler.cs
│   └── EventHandlers/
├── Infrastructure/
│   ├── Persistence/{Module}DbContext.cs
│   └── Repositories/
├── Endpoints/{Feature}Endpoints.cs
└── {Module}Module.cs
```

## Module Registration

```csharp
public sealed class CatalogModule : INacModule
{
    public string Name => "Catalog";
    public IReadOnlyList<Type> Dependencies => [];

    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddNacPersistence<CatalogDbContext>(config);
        services.AddNacMediator(x => x.AddHandlers(typeof(CatalogModule).Assembly));
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/catalog");
        ProductEndpoints.MapProductEndpoints(group);
    }
}
```

## Repository Pattern

```csharp
public sealed class GetProductsByPriceSpec : Specification<Product>
{
    public GetProductsByPriceSpec(decimal min, decimal max)
    {
        Query.Where(p => p.Price >= min && p.Price <= max)
             .OrderBy(p => p.Price)
             .Take(100);
    }
}

var products = await _repository.GetAsync(new GetProductsByPriceSpec(10, 100), ct);
```

## Permission Format

```
module.resource.action

Examples:
- catalog.products.create
- orders.*
- *.approve
```

## Pipeline Order

Command: ExceptionHandling → Logging → Validation → Authorization → TenantEnrichment → UnitOfWork → Handler → SaveChanges → DomainEvents

Query: ExceptionHandling → Logging → Validation → Authorization → CacheCheck → Handler → CacheStore

## Naming Conventions

- Commands: {Name}Command.cs + {Name}Handler.cs
- Queries: {Name}Query.cs + {Name}Handler.cs
- Entities: PascalCase, inherit AggregateRoot<Guid>
- Events: {Name}DomainEvent.cs or {Name}IntegrationEvent.cs
- Specs: {Name}Spec.cs

## Testing

```csharp
var fakeEventBus = new FakeEventBus();
var fakeUser = new FakeCurrentUser("user-id", ["orders.create"]);
var fakeTenant = new FakeTenantContext(tenantId);

var published = fakeEventBus.PublishedOf<OrderCreatedIntegrationEvent>();
Assert.NotEmpty(published);
```

## Exception Mapping

| Exception | HTTP |
|-----------|------|
| ValidationException | 400 |
| UnauthorizedException | 401 |
| ForbiddenException | 403 |
| NotFoundException | 404 |
| ConflictException | 409 |
| DomainException | 422 |
```
