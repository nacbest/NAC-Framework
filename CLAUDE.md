# NAC Framework

> Modular .NET 10 framework with CQRS, multi-tenancy, and clean architecture.

## Project Structure

```
src/
├── Nac.Abstractions/     — Contracts: ICurrentUser, IRepository, ICommand, etc.
├── Nac.Domain/           — Base types: AggregateRoot, Entity, ValueObject, DomainEvent
├── Nac.Mediator/         — CQRS dispatcher, pipeline behaviors
├── Nac.Persistence/      — EF Core base: NacDbContext, repositories, UoW
├── Nac.Persistence.PostgreSQL/ — PostgreSQL provider
├── Nac.Identity/         — [INFRA] ASP.NET Identity + JWT + tenant permissions
├── Nac.MultiTenancy/     — Tenant resolution, context, strategies
├── Nac.Caching/          — Distributed caching behaviors
├── Nac.Messaging/        — Event bus abstractions
├── Nac.Messaging.RabbitMQ/ — RabbitMQ implementation
├── Nac.Observability/    — Logging, tracing, health checks
├── Nac.WebApi/           — Minimal API helpers, exception handling
├── Nac.Auth/             — Authorization behaviors
├── Nac.Testing/          — Test fakes: FakeEventBus, FakeCurrentUser
└── Nac.Templates/        — dotnet new templates
skills/                   — AI-native scaffolding skills (nac-new, nac-add-module, etc.)
```

## Package Dependency Rules (CRITICAL)

```
┌───────────────────────────────────────────────────────────────┐
│                         Host Layer                             │
│   Composition root: wires DI, runs app                         │
│   Refs: Module.Infrastructure, Module (core), Nac.Identity     │
└─────────────────────────────┬─────────────────────────────────┘
                              │
┌─────────────────────────────┴─────────────────────────────────┐
│               Module Infrastructure Layer                      │
│   {Ns}.Modules.{M}.Infrastructure                              │
│   Refs: Nac.Persistence, Module (core)                         │
│   Owns: DbContext, Configurations, Repositories, DI extension  │
└─────────────────────────────┬─────────────────────────────────┘
                              │
┌─────────────────────────────┴─────────────────────────────────┐
│                 Module Core Layer (CLEAN)                       │
│   {Ns}.Modules.{M}                                             │
│   Refs: Nac.Abstractions, Nac.Domain, Nac.Mediator ONLY       │
│   Owns: Domain, Application, Contracts, Endpoints              │
└─────────────────────────────┬─────────────────────────────────┘
                              │
┌─────────────────────────────┴─────────────────────────────────┐
│                    Nac.Abstractions Layer                      │
│   ICurrentUser, IRepository, ICommand, ITenantContext          │
│   (Contracts only - NO implementation)                         │
└───────────────────────────────────────────────────────────────┘
```

**Dependency Rules:**
```
Host → Module.Infrastructure → Nac.Persistence
Host → Module.Infrastructure → Module (core)
Host → Module (core)
Module (core) → Nac.Abstractions, Nac.Domain, Nac.Mediator ONLY
Module (core) ✗ Nac.Persistence (FORBIDDEN)
Module (core) ✗ Module.Infrastructure (FORBIDDEN)
```

## Identity Linking Pattern

Business entities (Staff, Customer) link to NacUser via **Guid primitive only**:

```csharp
// ✅ CORRECT - Guid primitive, no navigation property
public sealed class Staff : AggregateRoot<Guid>
{
    public Guid UserId { get; set; }  // FK to NacUser.Id
    public required string EmployeeCode { get; init; }
    public required string Department { get; set; }
}

// ❌ WRONG - Navigation property couples to Infrastructure
public sealed class Staff : AggregateRoot<Guid>
{
    public NacUser User { get; set; }  // FORBIDDEN! Couples to Nac.Identity
}
```

### Why?

- `NacUser` lives in `Nac.Identity` — an Infrastructure package
- Business modules CANNOT reference Infrastructure
- FK constraint enforced at database level only (migrations or raw SQL)

### Access User Info

Use `ICurrentUser` from `Nac.Abstractions`:

```csharp
public class GetCurrentStaffQueryHandler : IQueryHandler<GetCurrentStaffQuery, StaffDto?>
{
    private readonly ICurrentUser _currentUser;  // From Nac.Abstractions
    private readonly IStaffRepository _staffRepo;

    public async Task<StaffDto?> Handle(GetCurrentStaffQuery query, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated) return null;
        var userId = Guid.Parse(_currentUser.UserId!);
        return await _staffRepo.GetByUserIdAsync(userId, ct);
    }
}
```

## Nac.Identity Components

| Component | Purpose |
|-----------|---------|
| `NacUser` | ASP.NET Identity user, global account |
| `TenantMembership` | Links user to tenant with role |
| `TenantRole` | Role + permissions scoped to tenant |
| `JwtCurrentUser` | Implements `ICurrentUser`, loads permissions from JWT + DB |
| `IJwtTokenService` | Generate/validate JWT tokens |
| `ITenantRoleService` | Manage tenant roles and memberships |

## Module Architecture (2-Project Pattern)

Each module splits into **core** (clean, persistence-ignorant) and **infrastructure** (EF Core, repositories):

**Module core** (`{Ns}.Modules.{M}`) — refs: `Nac.Abstractions`, `Nac.Domain`, `Nac.Mediator`:
```
{Ns}.Modules.Catalog/
├── Domain/Entities/Product.cs            — Entity inherits AggregateRoot<Guid>
├── Application/Commands/CreateProduct/   — Handler injects IRepository<Product>
├── Contracts/IProductRepository.cs       — Custom repo interface (optional)
└── Endpoints/ProductEndpoints.cs
```

**Module infrastructure** (`{Ns}.Modules.{M}.Infrastructure`) — refs: `Nac.Persistence`, Module core:
```
{Ns}.Modules.Catalog.Infrastructure/
├── CatalogDbContext.cs
├── CatalogInfrastructureExtensions.cs    — DI registration (1 line in Host)
├── Configurations/ProductConfiguration.cs
└── Repositories/ProductRepository.cs
```

**DbContext** (in `.Infrastructure`):
```csharp
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
```

**Host wiring** (1 line per module):
```csharp
// Program.cs
services.AddCatalogInfrastructure(connectionString);
```

**Rules:**
- Module core handlers inject `IRepository<T>` / `IReadRepository<T>` — never DbContext directly
- Module core never calls `SaveChangesAsync` — UnitOfWork behavior handles it
- Custom queries beyond `IRepository`: define interface in module core `Contracts/`, implement in `.Infrastructure`
- `AddNacRepositoriesFromAssembly` auto-scans `Entity<TId>` subtypes and registers repositories
- Each module owns its own DbContext and migrations

## Forbidden Patterns

- **Module core referencing Nac.Persistence** — only `.Infrastructure` can reference it
- **Module core referencing Nac.Identity** — use `ICurrentUser` from Nac.Abstractions
- **Module core referencing its own .Infrastructure** — dependency flows one way only
- **Navigation properties to NacUser** — use `Guid UserId` only
- **Cross-module DbContext access** — modules are isolated
- **Direct project references between modules** — use Integration Events
- **IQueryable exposure from repositories** — return concrete types only
- **Handlers calling SaveChanges** — UnitOfWork behavior handles it

## Skills (AI-Native Scaffolding)

NAC provides AI-native skills for Claude Code. Copy `skills/` to `~/.claude/skills/` to use.

```bash
/nac-new <Name>                    # New solution
/nac-add-module <Name>             # New module
/nac-add-feature <Module>/<Name>   # Command + Handler + Endpoint
/nac-add-entity <Module>/<Name>    # Entity in Domain/Entities/
/nac-install-identity              # Add Nac.Identity to Host project
/nac-install-caching               # Add Nac.Caching to Host project
/nac-install-messaging             # Add Nac.Messaging to Host project
/nac-install-observability         # Add Nac.Observability to Host project
```

Each skill:
- Reads context from `nac.json`
- Confirms operations via HARD-GATE
- Runs `dotnet build` to verify

## CQRS Pipeline

**Command:** ExceptionHandling → Logging → Validation → Authorization → TenantEnrichment → UnitOfWork → Handler → SaveChanges → DomainEvents

**Query:** ExceptionHandling → Logging → Validation → Authorization → CacheCheck → Handler → CacheStore

## Marker Interfaces

| Interface | Package | Purpose |
|-----------|---------|---------|
| `ICommand<T>` | Nac.Abstractions | Write operation |
| `IQuery<T>` | Nac.Abstractions | Read operation |
| `ITransactional` | Nac.Abstractions | Wrap in DB transaction |
| `IRequirePermission` | Nac.Abstractions | Check Permission property |
| `ICacheable` | Nac.Abstractions | Cache query result |
| `ICacheInvalidator` | Nac.Abstractions | Invalidate cache keys post-command |
| `IAuditable` | Nac.Abstractions | Log audit trail |

## Multi-Tenancy Strategies

| Strategy | Description |
|----------|-------------|
| Discriminator | TenantId column + EF global filter |
| Schema | Schema per tenant, switched at runtime |
| Database | Separate database per tenant |

## Permission Format

```
module.resource.action

Examples:
- catalog.products.create
- orders.*           (wildcard: all order permissions)
- *.approve          (wildcard: approve in any module)
```

## Testing

```csharp
// Use Fakes from Nac.Testing
var fakeEventBus = new FakeEventBus();
var fakeUser = new FakeCurrentUser("user-id", ["orders.create"]);
var fakeTenant = new FakeTenantContext("tenant-123");
```
