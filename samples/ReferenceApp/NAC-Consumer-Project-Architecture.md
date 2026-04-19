# Nac Consumer Project — Kiến trúc dự án sử dụng Framework

> Tài liệu kiến trúc cho các dự án SaaS/API được xây dựng trên **Nac Framework**.
> Áp dụng cho mọi project mới từ nay về sau.

---

> **You are reading the canonical copy bundled with the ReferenceApp sample.** When you clone+rename this folder as a new consumer project, keep this file alongside your solution as the living architecture spec.
>
> **Reference implementation:** [`./README.md`](./README.md) — this same sample (Orders + Billing + integration events, 11 integration tests green). Clone + rename thay cho `dotnet new`.

---

## 1. Tổng quan

Consumer project là dự án thực tế (SaaS, Web API) được xây dựng trên **Nac Framework**. Kiến trúc dựa trên **Modular Monolith** — mỗi feature là 1 module cô lập với ranh giới rõ ràng, có thể tách thành microservice khi cần scale.

Mỗi module gồm **2 projects**:
- **{Module}.Contracts** — public, ranh giới module, compiler-enforced
- **{Module}** — internal, chứa Domain + Features + Infrastructure trong folders

Layer boundaries trong module được enforce bởi Architecture Tests (namespace-level). Cross-module boundaries được enforce bởi compiler (Contracts project).

Mỗi module kế thừa `NacModule` base class với [DependsOn] dependency graph và vòng đời cấu hình rõ ràng.

---

## 2. Khởi tạo project

**Cách 1: Clone + Rename Reference Sample**

```bash
# Clone sample
git clone <repo> MyProject
cd MyProject

# Rename solution
mv ReferenceApp.sln MyProject.sln

# Rename namespaces
find . -type f -name "*.cs" -exec sed -i 's/ReferenceApp\./MyProject\./g' {} \;

# Xóa Orders/Billing nếu không cần
rm -rf src/Modules/Orders src/Modules/Billing
```

**Cách 2: Tạo mới từ đầu**

1. Tạo solution folder
2. Tạo `src/Host/` project (ASP.NET Core Web API)
3. Tạo `src/BuildingBlocks/MyProject.SharedKernel/` (Class Library)
4. Tạo module pair cho feature đầu tiên:
   - `src/Modules/Orders/Orders.Contracts/` (Class Library)
   - `src/Modules/Orders/Orders/` (Class Library)

---

## 3. Cấu trúc Solution

### 3.1 Layout 2-Project per Module

```
MyProject/
├── src/
│   ├── Host/                              # Entry point
│   │   ├── Program.cs                     # Composition root
│   │   ├── AppDbContext.cs                # Identity store (NacIdentityDbContext)
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs          # Login/register endpoints
│   │   │   └── ...custom controllers
│   │   ├── appsettings.json               # Config + connection strings
│   │   └── Host.csproj                    # Refs: Nac.WebApi, Nac.Identity, all modules
│   │
│   ├── BuildingBlocks/
│   │   └── MyProject.SharedKernel/        # Project-specific shared code
│   │       ├── Authorization/
│   │       │   └── PermissionAuthorizationPolicyProvider.cs
│   │       ├── Results/
│   │       │   └── ResultExtensions.cs    # Result<T> → IActionResult mapper
│   │       └── SharedKernel.csproj        # Refs: Nac.Core
│   │
│   └── Modules/
│       ├── Orders/
│       │   ├── Orders.Contracts/          # PUBLIC boundary
│       │   │   ├── DTOs/
│       │   │   │   ├── CreateOrderRequest.cs
│       │   │   │   └── OrderResponse.cs
│       │   │   ├── IntegrationEvents/
│       │   │   │   └── OrderCreatedEvent.cs
│       │   │   └── Contracts.csproj       # Refs: Nac.Core
│       │   │
│       │   └── Orders/                    # INTERNAL (all private/internal)
│       │       ├── Domain/
│       │       │   ├── Order.cs           # Aggregate root
│       │       │   ├── OrderItem.cs
│       │       │   ├── OrderStatus.cs     # Value object
│       │       │   └── OrderErrors.cs
│       │       │
│       │       ├── Features/              # Vertical slices per use case
│       │       │   ├── CreateOrder/
│       │       │   │   ├── CreateOrderCommand.cs
│       │       │   │   ├── CreateOrderHandler.cs
│       │       │   │   └── CreateOrderValidator.cs
│       │       │   ├── GetOrderById/
│       │       │   │   ├── GetOrderByIdQuery.cs
│       │       │   │   └── GetOrderByIdHandler.cs
│       │       │   └── EventHandlers/
│       │       │       └── UserRegisteredHandler.cs
│       │       │
│       │       ├── Controllers/
│       │       │   └── OrdersController.cs    # HTTP entry point (1 per module)
│       │       │
│       │       ├── Permissions/
│       │       │   └── OrderPermissionProvider.cs
│       │       │
│       │       ├── Infrastructure/
│       │       │   ├── OrdersDbContext.cs
│       │       │   ├── OrderConfiguration.cs
│       │       │   ├── Migrations/
│       │       │   └── OrderRepository.cs
│       │       │
│       │       ├── DataSeeding/
│       │       │   └── OrdersDataSeeder.cs
│       │       │
│       │       ├── OrdersModule.cs        # Module descriptor
│       │       └── Orders.csproj          # Refs: Nac.Core, Nac.Cqrs, Nac.Persistence, Orders.Contracts
│       │
│       └── Billing/
│           ├── Billing.Contracts/
│           └── Billing/
│               └── ... (cùng cấu trúc)
│
├── tests/
│   ├── Orders.Tests/
│   ├── Catalog.Tests/
│   └── Architecture.Tests/                # Module boundary enforcement
│
├── docker-compose.yml                     # (Optional) PostgreSQL, Redis — omit if using an external/managed instance
├── CLAUDE.md                              # AI rules
├── .editorconfig
└── MyProject.sln
```

> **Note:** Vertical slice đạt được thông qua Controllers nhỏ (1 action method = 1 use case). Controllers nằm trong module assembly, không ở Host.

---

## 4. Module Registration & Lifecycle

### 4.1 Module Class

```csharp
// Orders/OrdersModule.cs — kế thừa NacModule, khai báo [DependsOn]
[DependsOn(
    typeof(NacPersistenceModule),
    typeof(NacCqrsModule),
    typeof(NacEventBusModule))]
public sealed class OrdersModule : NacModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = context.Configuration;

        // 1. Persistence — AddNacPersistence registers DbContext, UnitOfWork,
        //    open-generic IRepository<T> / IReadRepository<T>, and enabled interceptors
        services.AddNacPersistence<OrdersDbContext>(opts =>
            opts
                .UseDbContext(builder => builder.UseNpgsql(
                    configuration.GetConnectionString("Default"),
                    npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "orders")))
                .EnableAuditInterceptor()
                .EnableOutbox());

        // 2. CQRS — handler assembly scan
        services.AddNacCqrs(opts =>
            opts.RegisterHandlersFromAssembly(typeof(OrdersModule).Assembly)
                .AddValidationBehavior()
                .AddTransactionBehavior());

        // 3. FluentValidation validators
        services.AddScoped<IValidator<CreateOrderCommand>, CreateOrderValidator>();

        // 4. Repositories
        services.AddScoped<IOrderRepository, OrderRepository>();

        // 5. Permissions
        services.AddSingleton<IPermissionDefinitionProvider, OrderPermissionProvider>();

        // 6. Data seeding
        services.AddScoped<IDataSeeder, OrdersDataSeeder>();

        // 7. Migration runner (Host resolves IEnumerable<IMigrationRunner> at startup)
        services.AddScoped<IMigrationRunner, OrdersMigrationRunner>();
    }
}
```

### 4.2 Explicit Registration

Không có convention scanner tự động. Tất cả registrations đều explicit trong `ConfigureServices`.

---

## 5. Quy tắc Module

### 5.1 Visibility

| Phần | Access | Ai thấy được |
|---|---|---|
| Contracts (project) | `public` | Tất cả modules + Host |
| Domain, Features, Permissions (folders) | `internal` | Chỉ trong module project |
| Infrastructure (folder) | `internal` | Chỉ trong module project |

### 5.2 Dependency Rules

```
Module A chỉ được reference:
  ✅ Nac.Core, Nac.Cqrs, Nac.Persistence, Nac.Caching, Nac.EventBus
  ✅ MyProject.SharedKernel
  ✅ ModuleB.Contracts (public DTOs/events)
  
  ❌ ModuleB internal project
  ❌ Nac.Identity (chỉ Host reference)
  ❌ Nac.WebApi (chỉ Host reference)
```

**Note:** Business modules reference framework packages trực tiếp, không cần reference Nac.WebApi vì Controllers nằm trong module assembly và `MapControllers()` ở Host auto-discover.

### 5.3 Enforcement (Architecture Tests)

```csharp
// Architecture.Tests/ModuleBoundaryTests.cs
[Fact]
public void Orders_ShouldNot_DependOn_Catalog_Internals()
{
    Types.InAssembly(typeof(OrdersModule).Assembly)
        .ShouldNot()
        .HaveDependencyOn("Modules.Catalog.Catalog")
        .GetResult()
        .IsSuccessful
        .Should().BeTrue();
}
```

---

## 6. Permissions

### 6.1 Nguyên tắc

Permissions là cross-cutting concern. Business modules **define permissions** via `IPermissionDefinitionProvider`, Host wires `IAuthorizationPolicyProvider` để dynamic build policies từ permission names.

### 6.2 Define Permissions

```csharp
// Orders/Permissions/OrderPermissionProvider.cs
public sealed class OrderPermissionProvider : IPermissionDefinitionProvider
{
    public void Define(IPermissionDefinitionContext context)
    {
        var group = context.AddGroup("Orders", "Order Management");

        group.AddPermission("Orders.View", "View orders");
        group.AddPermission("Orders.Create", "Create orders");
        group.AddPermission("Orders.Edit", "Edit orders");
        group.AddPermission("Orders.Delete", "Delete orders");
        group.AddPermission("Orders.Reports.Export", "Export order reports");
    }
}
```

> **Note:** Nested naming (e.g., `Orders.Reports.Export`) dùng flat dot notation. AddChild() không support trong v1.

### 6.3 Check Permissions — Attribute (Controller Action)

```csharp
// Orders/Controllers/OrdersController.cs
[ApiController]
[Route("api/orders")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "Orders.Create")]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        var command = new CreateOrderCommand(request.Items);
        var result = await sender.SendAsync<Nac.Core.Results.Result<Guid>>(command, ct);

        if (!result.IsSuccess)
            return result.ToActionResult();

        return CreatedAtAction(nameof(GetOrderById), new { id = result.Value }, result.Value);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Orders.View")]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken ct)
    {
        var query = new GetOrderByIdQuery(id);
        var result = await sender.SendAsync<Nac.Core.Results.Result<OrderResponse>>(query, ct);
        return result.ToActionResult();
    }
}
```

Dùng **standard ASP.NET Core** `[Authorize(Policy = "...")]` — NOT custom `[NacAuthorize]`.

### 6.4 Check Permissions — Programmatic (Handler)

```csharp
public sealed class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly IPermissionChecker _permissions;

    public async Task<Result<Guid>> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        if (!await _permissions.IsGrantedAsync("Orders.Create"))
            return Result.Forbidden();

        // Business logic...
    }
}
```

### 6.5 PermissionAuthorizationPolicyProvider (Host)

```csharp
// SharedKernel/Authorization/PermissionAuthorizationPolicyProvider.cs
internal sealed class PermissionAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallback.GetFallbackPolicyAsync();

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Fallback to built-in policies first
        var existingPolicy = await _fallback.GetPolicyAsync(policyName);
        if (existingPolicy is not null)
            return existingPolicy;

        // Treat unknown names as permission names
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();

        return policy;
    }
}
```

Host registers via `AddNacPermissionPolicies()` ở Program.cs.

### 6.6 Permission Management (Admin)

Nac.Identity provides built-in endpoints:

```
GET  /api/permissions?providerName=Role&providerKey=admin
PUT  /api/permissions
     Body: { providerName: "Role", providerKey: "admin", permissions: [...] }
```

---

## 7. Identity — Cách Consumer sử dụng

### 7.1 Nguyên tắc

Identity là **infrastructure concern** do framework cung cấp (Nac.Identity). Business modules **không reference Nac.Identity** — chỉ dùng interfaces từ Nac.Core.

```
Business Modules ──reference→ Nac.Core (interfaces)
                                  ↑
                                  │ implements
Host ──reference→ Nac.Identity ───┘ (registered vào DI tại startup)
```

### 7.2 Setup Identity

```csharp
// Program.cs
builder.Services.AddNacIdentity<AppDbContext>(opt =>
{
    opt.Jwt.SecretKey         = builder.Configuration["Jwt:SecretKey"]!;
    opt.Jwt.Issuer            = builder.Configuration["Jwt:Issuer"]!;
    opt.Jwt.Audience          = builder.Configuration["Jwt:Audience"]!;
    opt.Jwt.ExpirationMinutes = int.Parse(
        builder.Configuration["Jwt:ExpirationMinutes"] ?? "60");
});
```

Có ngay: identity tables, JWT auth, `/api/auth/*` endpoints, permission management.

**AppDbContext** kế thừa `NacIdentityDbContext` (không enable outbox/audit trên identity context):

```csharp
// Host/AppDbContext.cs
public sealed class AppDbContext : NacIdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Optional: add extra identity columns
    }
}
```

### 7.3 Login & Register Endpoints (Host)

```csharp
// Host/Controllers/AuthController.cs
[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<NacUser> userManager,
    JwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Email and Password are required." });

        var tenantId = request.TenantId ?? "default";
        var user = new NacUser(request.Email, tenantId)
        {
            FullName = request.FullName
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { userId = user.Id, email = user.Email, tenantId = user.TenantId });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Email and Password are required." });

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { error = "Invalid credentials." });

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            return Unauthorized(new { error = "Invalid credentials." });

        var token = await jwtTokenService.GenerateTokenAsync(user);
        return Ok(new { token, userId = user.Id, email = user.Email, tenantId = user.TenantId });
    }
}

public sealed record RegisterRequest(string Email, string Password, string? FullName = null, string? TenantId = null);
public sealed record LoginRequest(string Email, string Password);
```

### 7.4 Module dùng Identity data — qua interface

```csharp
// Orders module — chỉ reference Nac.Core interfaces, KHÔNG reference Nac.Identity
public sealed class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly ICurrentUser _currentUser;

    public async Task<Result<Guid>> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var order = Order.Create(
            customerId: _currentUser.Id,
            tenantId: _currentUser.TenantId
        );
        // ...
    }
}
```

---

## 8. Data Seeding

```csharp
// Orders/DataSeeding/OrdersDataSeeder.cs
public sealed class OrdersDataSeeder : IDataSeeder
{
    private readonly OrdersDbContext _db;

    public async Task SeedAsync(DataSeedContext context)
    {
        if (!await _db.OrderStatuses.AnyAsync())
        {
            _db.OrderStatuses.AddRange(
                new OrderStatus { Name = "Pending" },
                new OrderStatus { Name = "Processing" },
                new OrderStatus { Name = "Completed" }
            );
            await _db.SaveChangesAsync();
        }
    }
}
```

Seeders auto-registered via `services.AddScoped<IDataSeeder, OrdersDataSeeder>()` và chạy tự động trong Host startup (Development mode).

---

## 9. CQRS Pipeline

### 9.1 Write Path (Command)

```
HTTP Request
    → Controller action
    → ISender.SendAsync<Result<T>>(command, ct)
        → ValidationBehavior (FluentValidation)
        → TransactionBehavior (begin transaction)
        → CommandHandler.Handle()
            → Load aggregate từ repository
            → Call domain method
            → Aggregate.AddDomainEvent()
            → SaveChanges()
        → OutboxInterceptor (harvest events → write outbox rows)
        → Commit transaction
    → Return Result<T>
    → Controller calls result.ToActionResult()
```

**Key APIs:**
- `ISender.SendAsync<Result<T>>(request, ct)` — CQRS dispatch (NOT `Dispatcher.Send`)
- `Result<T>.ToActionResult()` — maps Result to IActionResult (defined in SharedKernel)

### 9.2 Read Path (Query)

```
HTTP Request
    → Controller action
    → ISender.SendAsync<OrderResponse>(query, ct)
        → CachingBehavior (check HybridCache)
        → QueryHandler.Handle()
            → EF Core query projection
        → Cache result
    → Return DTO directly
```

---

## 10. Multi-tenancy

### Layer 1 — Tenant Resolution (Middleware)

```
HTTP Request Header/JWT
    → TenantResolutionMiddleware
    → ITenantContext.SetCurrentTenant()
    → Available everywhere via DI
```

Strategies (chain theo thứ tự):
1. JWT claim `tenant_id` — API requests
2. Custom header `X-Tenant-Id` — API requests
3. Custom delegate — background jobs

Setup in Program.cs:

```csharp
builder.Services.AddNacMultiTenancy(opt =>
{
    opt.DefaultTenantId = "default";
    opt.Strategies.Add(typeof(HeaderTenantStrategy));
});

builder.Services.AddSingleton<ITenantStore>(new InMemoryTenantStore([
    new TenantInfo { Id = "default", Name = "Default Tenant", IsActive = true }
]));
```

### Layer 2 — Application Filter (EF Core)

EF Core Global Query Filters (Named filters):

```csharp
// OrdersDbContext.OnModelCreating()
modelBuilder.ApplyTenantFilter<Order>();
modelBuilder.ApplyTenantFilter<OrderItem>();
// Filters: TenantId = @currentTenant (always ON)
```

### Layer 3 — Roadmap: Database-Level Defense (RLS)

> Row-Level Security via PostgreSQL policies planned for v2+. Currently Layer 1 + 2 provide tenant isolation sufficient for v1 SaaS deployments.

### Premium Tenant — Database-per-tenant (Optional)

Implement custom `ITenantConnectionStringResolver`:

```csharp
public sealed class DatabasePerTenantResolver : ITenantConnectionStringResolver
{
    public string Resolve(string tenantId)
    {
        return tenantId switch
        {
            "premium-1" => "Host=db;Database=premium_1_db",
            _ => "Host=db;Database=shared_db"
        };
    }
}
```

---

## 11. Event Flow

### 11.1 Single Event Pattern (Domain + Integration)

One event class implements both `IDomainEvent` + `IIntegrationEvent`:

```csharp
// Orders.Contracts/IntegrationEvents/OrderCreatedEvent.cs
public sealed record OrderCreatedEvent(
    Guid OrderId,
    Guid CustomerId,
    string TenantId,
    decimal Total,
    DateTime OccurredOn,
    Guid EventId) : IDomainEvent, IIntegrationEvent;
```

**Flow:**
1. Aggregate raises: `Order.AddDomainEvent(new OrderCreatedEvent(...))`
2. SaveChanges() → OutboxInterceptor catches IDomainEvent → writes outbox row
3. Background OutboxWorker polls outbox → publishes via IIntegrationEventPublisher
4. Billing module handler: `IEventHandler<OrderCreatedEvent>.HandleAsync()`

### 11.2 Event Handler Pattern

```csharp
// Billing/Features/EventHandlers/OrderCreatedEventHandler.cs
internal sealed class OrderCreatedEventHandler(
    BillingDbContext db,
    ITenantContext tenant)
    : IEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event, CancellationToken ct = default)
    {
        // Restore tenant context — background worker loses AsyncLocal propagation
        tenant.SetCurrentTenant(new TenantInfo
        {
            Id   = @event.TenantId,
            Name = @event.TenantId,
        });

        // Idempotency check
        var invoiceExists = await db.Invoices
            .AnyAsync(i => i.OrderId == @event.OrderId, ct);
        if (invoiceExists)
            return;

        // Create invoice
        var invoice = new Invoice
        {
            Id         = Guid.NewGuid(),
            OrderId    = @event.OrderId,
            Amount     = @event.Total,
            Status     = InvoiceStatus.Pending,
            TenantId   = @event.TenantId,
            CreatedAt  = DateTime.UtcNow,
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
    }
}
```

**Pattern:** `IEventHandler<T>` with `HandleAsync()` method. Framework auto-discovers + registers via assembly scan in `AddNacEventBus()`.

### 11.3 Register Event Handlers

In module ConfigureServices:

```csharp
builder.Services.AddNacEventBus(opt =>
    opt.RegisterHandlersFromAssembly(typeof(BillingModule).Assembly)
       .UseInMemoryTransport());
```

---

## 12. Database Strategy

### Schema-per-Module

```
PostgreSQL Database: myproject_db
├── identity schema   → AppDbContext
│   ├── users
│   ├── roles
│   └── permission_grants
├── orders schema     → OrdersDbContext
│   ├── orders
│   ├── order_items
│   └── __EFMigrationsHistory
├── billing schema    → BillingDbContext
│   ├── invoices
│   └── __EFMigrationsHistory
└── public schema     → outbox, tenant config
```

### Migration Commands

```bash
# Orders module
dotnet ef migrations add Init \
  --project src/Modules/Orders/Orders \
  --startup-project src/Host \
  --context OrdersDbContext \
  --output-dir Infrastructure/Migrations

dotnet ef database update --project src/Host --context OrdersDbContext
```

Host manages migrations via `IMigrationRunner` at startup:

```csharp
// Program.cs (Development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var runners = scope.ServiceProvider.GetServices<IMigrationRunner>();
    foreach (var runner in runners)
        await runner.RunAsync();
}
```

---

## 13. Infrastructure Stack

### Development

Consumer points `ConnectionStrings:Default` at an externally-provisioned **Postgres 17** instance (local install, managed service, or a user-owned docker container). The ReferenceApp sample intentionally does NOT ship a `docker-compose.yml` — Postgres lifecycle is the operator's choice. Sample defaults assume `localhost:5432`, user `admin`, password `123456`, database `referenceapp` — override via `ConnectionStrings__Default` env var.

If you want a one-command dev DB, drop a local `docker-compose.yml` next to your solution, e.g.:

```yaml
services:
  postgres:
    image: postgres:17
    environment:
      POSTGRES_DB: myproject_db
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: changeme
    ports:
      - "5432:5432"
```

Redis is **not** required in v1 — `AddNacCaching()` uses an in-memory HybridCache by default. Add a Redis service only when the framework exposes L2 backing (roadmap §17).

### Production

```
API Layer:
  - ASP.NET Core       — Controllers + middleware

Data Layer:
  - PostgreSQL         — Primary database (managed, pooled, SSL)
  - Redis              — L2 cache (v2+, once framework supports it)

Messaging:
  - RabbitMQ / Kafka   — Integration events (when extracting to microservices)

Observability:
  - Serilog            — Structured logs
  - OpenTelemetry      — Tracing (optional)
```

---

## 14. Naming Conventions

| Type | Pattern | Example |
|---|---|---|
| Entity | `{Name}.cs` | `Order.cs` |
| Command | `{Verb}{Name}Command.cs` | `CreateOrderCommand.cs` |
| Command Handler | `{Verb}{Name}Handler.cs` | `CreateOrderHandler.cs` |
| Query | `Get{Name}ByIdQuery.cs` | `GetOrderByIdQuery.cs` |
| Query Handler | `Get{Name}ByIdHandler.cs` | `GetOrderByIdHandler.cs` |
| Validator | `{Command/Query}Validator.cs` | `CreateOrderValidator.cs` |
| Event (merged domain + integration) | `{Name}{Verb}Event.cs` — one record implementing both `IDomainEvent` + `IIntegrationEvent` (see §11.1) | `OrderCreatedEvent.cs` |
| Repository Interface | `I{Name}Repository.cs` | `IOrderRepository.cs` |
| Repository Impl | `{Name}Repository.cs` | `OrderRepository.cs` |
| DbContext | `{Module}DbContext.cs` | `OrdersDbContext.cs` |
| Controller | `{Module}Controller.cs` | `OrdersController.cs` |
| DTO Request | `{Verb}{Name}Request.cs` | `CreateOrderRequest.cs` |
| DTO Response | `{Name}Response.cs` | `OrderResponse.cs` |
| Permission Provider | `{Module}PermissionProvider.cs` | `OrderPermissionProvider.cs` |
| Data Seeder | `{Module}DataSeeder.cs` | `OrdersDataSeeder.cs` |
| Module Descriptor | `{Module}Module.cs` | `OrdersModule.cs` |

---

## 15. Host Program.cs (Composition Root)

```csharp
using Billing;
using Microsoft.EntityFrameworkCore;
using Orders.Contracts.IntegrationEvents;
using Nac.Caching.Extensions;
using Nac.Core.Abstractions;
using Nac.Core.DataSeeding;
using Nac.Core.Extensions;
using Nac.Cqrs.Extensions;
using Nac.EventBus.Extensions;
using Nac.Identity.Extensions;
using Nac.MultiTenancy.Extensions;
using Nac.Persistence.Extensions;
using Nac.WebApi.Extensions;
using Orders;
using MyProject.Host;

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure primitives ──────────────────────────────────────────
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddHybridCache();

// ── WebApi options (must precede AddNacApplication) ──────────────────
builder.Services.AddNacWebApi(opt =>
{
    opt.EnableOpenApi = true;
    opt.EnableCors = true;
    opt.EnableHealthChecks = true;
});

// ── Persistence: Host-owned identity context ──────────────────────────
builder.Services.AddNacPersistence<AppDbContext>(opts =>
    opts.UseDbContext(b => b.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "identity"))));

// ── Multi-tenancy ──────────────────────────────────────────────────────
builder.Services.AddNacMultiTenancy(opt =>
{
    opt.DefaultTenantId = builder.Configuration["MultiTenancy:DefaultTenantId"] ?? "default";
    opt.Strategies.Add(typeof(HeaderTenantStrategy));
});

builder.Services.AddSingleton<ITenantStore>(new InMemoryTenantStore([
    new TenantInfo { Id = "default", Name = "Default Tenant", IsActive = true }
]));

// ── CQRS pipeline ──────────────────────────────────────────────────────
builder.Services.AddNacCqrs(c =>
    c.AddLoggingBehavior()
     .AddValidationBehavior()
     .AddCachingBehavior()
     .AddTransactionBehavior());

// ── EventBus: scan module assemblies ───────────────────────────────────
builder.Services.AddNacEventBus(opt =>
    opt.RegisterHandlersFromAssembly(typeof(OrdersModule).Assembly)
       .RegisterHandlersFromAssembly(typeof(BillingModule).Assembly)
       .RegisterHandlersFromAssembly(typeof(OrderCreatedEvent).Assembly)
       .UseInMemoryTransport());

// ── Identity + JWT ─────────────────────────────────────────────────────
builder.Services.AddNacIdentity<AppDbContext>(opt =>
{
    opt.Jwt.SecretKey         = builder.Configuration["Jwt:SecretKey"]!;
    opt.Jwt.Issuer            = builder.Configuration["Jwt:Issuer"]!;
    opt.Jwt.Audience          = builder.Configuration["Jwt:Audience"]!;
    opt.Jwt.ExpirationMinutes = int.Parse(
        builder.Configuration["Jwt:ExpirationMinutes"] ?? "60");
});

// ── Caching ────────────────────────────────────────────────────────────
builder.Services.AddNacCaching();

// ── Permission policy provider ─────────────────────────────────────────
builder.Services.AddNacPermissionPolicies();

// ── Module system ──────────────────────────────────────────────────────
builder.Services.AddNacApplication<AppRootModule>(builder.Configuration);

// ────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseNacApplication();
app.MapControllers();
app.Run();

public partial class Program { }
```

**Key ordering rules:**
1. Infrastructure primitives first (IDateTimeProvider, HybridCache)
2. AddNacWebApi before AddNacApplication
3. AddNacPersistence before AddNacIdentity
4. AddNacApplication last (discovers all [DependsOn] modules)
5. UseNacApplication → adds middleware pipeline
6. MapControllers → discovers all Controllers across module assemblies

---

## 16. Path to Microservice

```
Phase 1: Modular Monolith (v1)
  - Tất cả modules trong 1 process
  - Cross-module via local outbox
  - Shared PostgreSQL, schema-per-module

Phase 2: Extract Hot Module (v2)
  - Tách module → separate service
  - Outbox → RabbitMQ transport
  - Handler code KHÔNG ĐỔI

Phase 3: Full Microservices (v3+)
  - Mỗi module = 1 service
  - YARP gateway
  - Separate database per service
```

---

## 17. Roadmap (v2+)

### Planned Features

| Feature | Status | Notes |
|---------|--------|-------|
| **IEndpoint abstraction** | Planned | Vertical slice endpoints with compile-time discovery |
| **[NacAuthorize] attribute** | Planned | Alternative to standard [Authorize(Policy=...)] |
| **RLS interceptor** | Planned | Database-level row-level security (Layer 3) |
| **HybridCache + Redis** | Partial | In-memory only in v1; Redis backing planned |
| **Architecture.Tests NetArchTest** | Planned | Boundary enforcement templates |
| **dotnet new templates** | Planned | `dotnet new nac-solution`, `nac-module` |
| **Generic OutboxWorker<TContext>** | Planned | Per-module background job framework |

### v1 Limitations

- Controllers required (no endpoint abstraction)
- Standard ASP.NET Core `[Authorize]` only
- Tenant isolation via middleware + EF filters (no RLS)
- HybridCache in-memory default (Redis optional, manual setup)
- No pre-built Architecture.Tests templates
- Manual scaffolding or ReferenceApp clone (no `dotnet new`)

---

## Further Reading

- **Reference Implementation:** [`samples/ReferenceApp/README.md`](./samples/ReferenceApp/README.md)
- **Framework Documentation:** `/docs/` directory (if any)
- **Nac.Core Abstractions:** Events, Results, CQRS, Permissions
- **Nac.Identity:** JWT, UserManager, Permission Grants
- **Nac.Persistence:** DbContext, Interceptors, Outbox
