# Nac Framework — Kiến trúc Framework Project

> Tài liệu kiến trúc cho việc build và maintain Nac Framework dưới dạng bộ NuGet packages tái sử dụng.
> Target: .NET 10 LTS | PostgreSQL | MIT License

---

## 1. Tổng quan

Nac Framework là một **mono-repo** chứa toàn bộ source code, templates, AI rules, examples và tests. Mỗi thư mục trong `src/` build thành **1 NuGet package riêng biệt**, publish lên private NuGet feed. Consumer project chỉ reference qua NuGet — không bao giờ copy source.

---

## 2. Cấu trúc Repository

```
nac-framework/
├── src/                          # NuGet packages (mỗi folder = 1 package)
│   ├── Nac.Core/                 # L0 — Zero dependencies, DDD building blocks
│   ├── Nac.Cqrs/                 # L1 — Custom dispatcher, behaviors
│   ├── Nac.Caching/              # L1 — HybridCache wrapper
│   ├── Nac.Persistence/          # L2 — EF Core base, interceptors
│   ├── Nac.MultiTenancy/         # L2 — Finbuckle + PostgreSQL RLS
│   ├── Nac.EventBus/             # L2 — Outbox, integration events
│   ├── Nac.Identity/             # L2 — Auth + Identity + Permissions
│   ├── Nac.Observability/        # L2 — OpenTelemetry + Serilog
│   ├── Nac.Jobs/                 # L2 — Hangfire wrapper
│   ├── Nac.Testing/              # L2 — Test helpers, fixtures
│   └── Nac.WebApi/               # L3 — Composition root, DI wiring
│
├── templates/                    # dotnet new templates
│   ├── Nac.Templates.csproj      # Template pack project
│   ├── nac-solution/             # dotnet new nac-solution
│   ├── nac-module/               # dotnet new nac-module -n {Name}
│   ├── nac-entity/               # dotnet new nac-entity -n {Name}
│   └── nac-endpoint/             # dotnet new nac-endpoint -n {Name}
│
├── ai-rules/                     # AI instruction files (source of truth)
│   ├── conventions.md            # Tool-agnostic rules
│   ├── CLAUDE.md                 # Claude Code instructions
│   ├── .cursor/rules/            # Cursor rules (.mdc files)
│   ├── copilot-instructions.md   # GitHub Copilot
│   └── AGENTS.md                 # Generic AI agents
│
├── examples/                     # Canonical reference projects
│   ├── SimpleCrud/               # Basic entity CRUD
│   ├── SaaSStarter/              # Multi-tenant SaaS complete
│   └── MicroserviceExtract/      # Module extracted to service
│
├── tests/
│   ├── Nac.Core.Tests/
│   ├── Nac.Cqrs.Tests/
│   ├── Nac.Persistence.Tests/
│   ├── Nac.Identity.Tests/
│   ├── Nac.Integration.Tests/    # Cross-package integration
│   └── Nac.Architecture.Tests/   # NetArchTest boundary enforcement
│
├── docs/                         # Framework documentation
│   ├── getting-started.md
│   ├── module-guide.md
│   ├── identity.md
│   ├── multi-tenancy.md
│   └── migration-to-microservice.md
│
├── build/                        # Build scripts, CI config
│   ├── Directory.Build.props     # Shared MSBuild props
│   ├── Directory.Packages.props  # Central Package Management
│   └── ci.yml                    # GitHub Actions / Azure DevOps
│
├── NacFramework.sln
├── Directory.Build.props         # Root build properties
├── Directory.Packages.props      # Central version pinning
├── nuget.config                  # Private feed config
└── README.md
```

---

## 3. Package Dependency Graph

Nguyên tắc: **mũi tên chỉ đi xuống, không bao giờ đi ngược lên**. Không circular dependencies.

```
L0 — Zero Dependencies
┌──────────────────────────────────────────────────────────┐
│                       Nac.Core                            │
│                  (no external deps)                       │
│                                                          │
│  Primitives/                                             │
│    Entity<TId>, AggregateRoot<TId>, ValueObject,         │
│    StronglyTypedId<T>, IDomainEvent                      │
│                                                          │
│  Domain/                                                 │
│    IRepository<T>, IReadRepository<T>,                   │
│    Specification<T>, DomainError, Guard                   │
│                                                          │
│  Results/                                                │
│    Result<T>, Result (Ardalis.Result)                     │
│                                                          │
│  Abstractions/                                           │
│    IIdentityService, ICurrentUser, UserInfo,              │
│    UserRegisteredEvent, UserEmailConfirmedEvent,          │
│    PasswordResetEvent, IDateTimeProvider                  │
│    IPermissionChecker, IPermissionDefinitionProvider,     │
│    PermissionDefinition, PermissionGroup                  │
│                                                          │
│  DependencyInjection/                                    │
│    ITransientDependency, IScopedDependency,               │
│    ISingletonDependency, DependencyAttribute              │
│                                                          │
│  Modularity/                                             │
│    NacModule, DependsOnAttribute,                         │
│    ServiceConfigurationContext,                           │
│    ApplicationInitializationContext,                      │
│    ApplicationShutdownContext                              │
│                                                          │
│  DataSeeding/                                            │
│    IDataSeeder, DataSeedContext                            │
│                                                          │
│  ValueObjects/                                           │
│    Money, Address, DateRange, Pagination                  │
└────────┬─────────────────────────────────────────────────┘
         │
L1 — Core Dependencies Only
┌────────┴──────┐  ┌──────────────┐
│   Nac.Cqrs    │  │ Nac.Caching  │
│ (Core only)   │  │(Core+Hybrid) │
│               │  │              │
│ ICommand<T>   │  │ ITenantCache │
│ IQuery<T>     │  │ CacheKeys    │
│ Dispatcher    │  │ TagInvalidate│
│ IPipeline     │  │              │
└──┬────────┬───┘  └──────────────┘
   │        │
L2 — Infrastructure Packages
┌──┴────────┴──┐  ┌──────────────┐  ┌──────────────┐
│Nac.Persistence│  │Nac.MultiTen. │  │ Nac.EventBus │
│(CQRS+EFCore) │  │(Core+Finb.)  │  │(CQRS+Wolver.)│
│              │  │              │  │              │
│ NacDbContext │  │ TenantResolve│  │ IEventBus    │
│ UnitOfWork   │  │ RLS Setup    │  │ OutboxWorker │
│ Interceptors │  │ PerTenantConn│  │ InboxConsumer│
│ Repository<T>│  │ NamedFilters │  │              │
│ ConventionReg│  │              │  │              │
└──────────────┘  └──────────────┘  └──────────────┘

┌──────────────────────────────────────────────────────┐
│              Nac.Identity (L2)                         │
│  (Auth + Identity + Authorization)                     │
│                                                        │
│  Depends on: Nac.Core, Nac.Persistence,                │
│    Nac.MultiTenancy, Nac.EventBus,                     │
│    Microsoft.AspNetCore.Identity.EntityFrameworkCore    │
│                                                        │
│  Implements:                                           │
│    IIdentityService, IPermissionChecker                │
│    TenantAwareUserManager, JWT tokens,                 │
│    PermissionStore, PermissionManager,                  │
│    Built-in auth + permission endpoints                │
│    Optional Keycloak SSO                               │
└──────────────────────────────────────────────────────┘

┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│Nac.Observ.   │  │   Nac.Jobs   │  │ Nac.Testing  │
│(Core+OTel)   │  │(Core+Hangfire│  │ (all above)  │
│              │  │              │  │              │
│ OTel Config  │  │ TenantAware  │  │ TestContainers│
│ Serilog Enr. │  │ Job Scheduler│  │ FakeProviders │
│ HealthChecks │  │ RetryPolicy  │  │ ArchTests     │
└──────────────┘  └──────────────┘  └──────────────┘

L3 — Composition Root
┌─────────────────────────────────────────────┐
│              Nac.WebApi                       │
│  (references all L2, wires DI, middleware)   │
│                                              │
│  AddNacFramework() extension method          │
│  UseNacFramework() middleware pipeline       │
│  Module graph resolution (topo-sort)         │
│  Convention-based auto-registration          │
│  OpenAPI + Scalar config                     │
│  YARP gateway config                         │
│  Global exception handling                   │
└──────────────────────────────────────────────┘
```

### Nguyên tắc Abstraction Boundary

```
Business modules (Orders, Billing…) chỉ reference Nac.Core
    → Thấy: IIdentityService, ICurrentUser, UserInfo, Events,
            IPermissionChecker, PermissionDefinition
    → KHÔNG thấy: UserManager, DbContext, JWT internals, PermissionStore

Nac.Identity implements IIdentityService + IPermissionChecker
    → Register vào DI container tại startup
    → Consumer modules nhận interface qua DI, không biết implementation

Chỉ Host (composition root) reference Nac.Identity trực tiếp
    → Khi cần: UseIdentity<AppUser>(), custom endpoints, extend user
```

---

## 4. Chi tiết từng Package

### 4.1 Nac.Core (L0)

**Mục đích:** Base types dùng chung, DDD building blocks, modularity infrastructure. Không phụ thuộc bất kỳ thư viện ngoài nào (trừ Ardalis.Result).

#### Primitives

- `Entity<TId>` — Base entity với strongly-typed ID
- `AggregateRoot<TId>` — Entity + domain event collection
- `ValueObject` — Immutable value comparison
- `StronglyTypedId<T>` — Wrapper cho Guid/long/string IDs
- `IDomainEvent` — Marker interface cho domain events

#### Domain Building Blocks

- `IRepository<T>` / `IReadRepository<T>` — Repository abstractions
- `Specification<T>` — Specification pattern cho queries
- `DomainError` — Typed domain errors
- `Guard` — Fluent guard clauses cho domain validation

#### Results

- `Result<T>` / `Result` — Railway-oriented error handling (Ardalis.Result)

#### Abstractions (Identity)

```csharp
public interface IIdentityService
{
    Task<UserInfo?> GetUserInfoAsync(Guid userId);
    Task<IReadOnlyList<UserInfo>> GetUsersAsync(IEnumerable<Guid> userIds);
    Task<bool> IsInRoleAsync(Guid userId, string role);
}

public record UserInfo(
    Guid Id, string Email, string? FullName,
    string TenantId, IReadOnlyList<string> Roles
);

public record UserRegisteredEvent(Guid UserId, string Email, string TenantId)
    : IIntegrationEvent;
public record UserEmailConfirmedEvent(Guid UserId, string TenantId)
    : IIntegrationEvent;
public record PasswordResetEvent(Guid UserId, string TenantId)
    : IIntegrationEvent;
```

#### Abstractions (Permissions)

```csharp
public interface IPermissionChecker
{
    Task<bool> IsGrantedAsync(string permissionName);
    Task<bool> IsGrantedAsync(Guid userId, string permissionName);
    Task<MultiplePermissionGrantResult> IsGrantedAsync(string[] permissionNames);
}

public interface IPermissionDefinitionProvider
{
    void Define(IPermissionDefinitionContext context);
}

public class PermissionDefinition
{
    public string Name { get; }
    public string? DisplayName { get; }
    public bool IsEnabled { get; set; } = true;
    public List<PermissionDefinition> Children { get; }
}

public class PermissionGroup
{
    public string Name { get; }
    public string? DisplayName { get; }
    public List<PermissionDefinition> Permissions { get; }
    public PermissionDefinition AddPermission(string name, string? displayName = null);
}
```

#### Dependency Injection Markers

```csharp
// Marker interfaces — framework auto-registers implementors
public interface ITransientDependency { }
public interface IScopedDependency { }
public interface ISingletonDependency { }

// Override framework service
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DependencyAttribute : Attribute
{
    public bool ReplaceServices { get; set; }
    public ServiceLifetime? Lifetime { get; set; }
}
```

#### Modularity

```csharp
// Base class cho tất cả modules
public abstract class NacModule
{
    // Phase 1: Pre — interceptors, option customization
    public virtual void PreConfigureServices(ServiceConfigurationContext context) { }

    // Phase 2: Main — register services
    public virtual void ConfigureServices(ServiceConfigurationContext context) { }

    // Phase 3: Post — validate, adjust after all modules registered
    public virtual void PostConfigureServices(ServiceConfigurationContext context) { }

    // Phase 4: App init — middleware, seed data, warmup
    public virtual void OnApplicationInitialization(ApplicationInitializationContext context) { }

    // Phase 5: Shutdown — cleanup
    public virtual void OnApplicationShutdown(ApplicationShutdownContext context) { }
}

// Khai báo dependency giữa modules
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DependsOnAttribute : Attribute
{
    public Type[] DependedModuleTypes { get; }
    public DependsOnAttribute(params Type[] dependedModuleTypes)
    {
        DependedModuleTypes = dependedModuleTypes;
    }
}
```

#### Data Seeding

```csharp
public interface IDataSeeder
{
    Task SeedAsync(DataSeedContext context);
}

public class DataSeedContext
{
    public IServiceProvider ServiceProvider { get; }
    public string? TenantId { get; set; }
    public IDictionary<string, object?> Properties { get; }
}
```

#### Common

- `IDateTimeProvider` — Testable time abstraction
- `ICurrentUser` — Current user context interface
- Common value objects: `Money`, `Address`, `DateRange`, `Pagination`

### 4.2 Nac.Cqrs (L1)

**Mục đích:** Custom CQRS dispatcher tự viết, thay thế MediatR.

Chứa:
- `ICommand<TResult>` / `IQuery<TResult>` — Marker interfaces
- `ICommandHandler<TCommand, TResult>` / `IQueryHandler<TQuery, TResult>`
- `Dispatcher` — FrozenDictionary-based, cached delegates, zero reflection sau lần đầu
- `IPipelineBehavior<TRequest, TResult>` — Middleware chain
- Built-in behaviors: `LoggingBehavior`, `ValidationBehavior`, `TransactionBehavior`
- `INotification` / `INotificationHandler<T>` — Pub/sub in-process

### 4.3 Nac.Caching (L1)

**Mục đích:** HybridCache wrapper, tenant-aware.

Chứa:
- `ITenantCache` — Tenant-scoped cache operations
- `CacheKeys` — Convention-based cache key generation
- Tag-based invalidation
- L1 (memory) + L2 (Redis) + stampede protection

### 4.4 Nac.Persistence (L2)

**Mục đích:** EF Core integration.

Chứa:
- `NacDbContext` — Base DbContext với audit fields, soft delete, tenant filter
- `Repository<T>` — Generic repository implementation
- `UnitOfWork` — Transaction wrapper
- `DomainEventInterceptor` — SaveChanges → dispatch domain events
- `AuditInterceptor` — Auto-fill CreatedAt, UpdatedAt, CreatedBy
- `SoftDeleteInterceptor` — Override Delete → set IsDeleted
- EF Core conventions: snake_case naming, UTC DateTime, enum-to-string
- Convention-based service registration for this package's types

### 4.5 Nac.MultiTenancy (L2)

**Mục đích:** Multi-tenant isolation.

Chứa:
- Finbuckle integration + custom strategies (JWT claim, subdomain, message header)
- EF Core named query filters setup: `Tenant` + `SoftDelete`
- PostgreSQL RLS policy generator
- `DbConnectionInterceptor` — SET LOCAL app.current_tenant per request
- Per-tenant connection string support
- `ITenantContext` — Resolve current tenant anywhere

### 4.6 Nac.EventBus (L2)

**Mục đích:** Integration events giữa modules.

Chứa:
- `IIntegrationEvent` — Cross-module event marker
- `IEventBus` — Publish interface
- Outbox table + `OutboxProcessor` background worker
- Inbox table cho idempotent consumption
- Wolverine transport abstraction (local → RabbitMQ switch via config)
- `IntegrationEventHandler<T>` — Base handler

### 4.7 Nac.Identity (L2)

**Mục đích:** Tenant-aware authentication, user management, authorization, permissions. Gộp Auth + Identity + Authorization trong cùng 1 package.

**Depends on:** Nac.Core, Nac.Persistence, Nac.MultiTenancy, Nac.EventBus, Microsoft.AspNetCore.Identity.EntityFrameworkCore

```
src/Nac.Identity/
├── Core/
│   ├── NacIdentityUser.cs              # Base user: IdentityUser + ITenantEntity + IAuditable
│   ├── NacIdentityRole.cs              # Base role + TenantId
│   └── NacRefreshToken.cs              # Refresh token entity
│
├── Persistence/
│   ├── NacIdentityDbContext.cs          # Generic: NacIdentityDbContext<TUser>
│   ├── Configurations/
│   │   ├── IdentityUserConfiguration.cs
│   │   ├── IdentityRoleConfiguration.cs
│   │   ├── RefreshTokenConfiguration.cs
│   │   └── PermissionGrantConfiguration.cs
│   └── Migrations/                      # Framework ships base migrations
│
├── Services/
│   └── IdentityService.cs              # Implements IIdentityService (từ Nac.Core)
│
├── MultiTenancy/
│   ├── TenantAwareUserManager.cs        # Override FindByEmail → scope by tenant
│   ├── TenantAwareSignInManager.cs      # Tenant context khi sign-in
│   └── TenantUserValidator.cs           # Unique email PER tenant, không global
│
├── Tokens/
│   ├── JwtTokenService.cs               # Generate + validate JWT + tenant_id claim
│   ├── RefreshTokenService.cs           # Rotate refresh tokens
│   └── TokenConfiguration.cs
│
├── Authorization/
│   ├── Permissions/
│   │   ├── PermissionGrant.cs           # Entity: Provider, ProviderKey, PermissionName, TenantId
│   │   ├── PermissionStore.cs           # EF Core persistence (implements IPermissionStore)
│   │   ├── PermissionChecker.cs         # Implements IPermissionChecker (từ Nac.Core)
│   │   └── PermissionManager.cs         # CRUD operations on grants
│   ├── Policies/
│   │   ├── PermissionPolicyProvider.cs  # Maps permission names → ASP.NET policies
│   │   ├── PermissionAuthorizationHandler.cs
│   │   └── NacAuthorizeAttribute.cs     # [NacAuthorize("Orders.Create")]
│   └── Claims/
│       └── TenantClaimsTransformation.cs
│
├── Keycloak/                            # Optional SSO integration
│   ├── KeycloakOptions.cs
│   └── KeycloakExtensions.cs
│
├── Endpoints/
│   ├── Auth/
│   │   ├── RegisterEndpoint.cs          # POST /api/auth/register
│   │   ├── LoginEndpoint.cs             # POST /api/auth/login
│   │   ├── RefreshEndpoint.cs           # POST /api/auth/refresh
│   │   ├── MeEndpoint.cs               # GET  /api/auth/me
│   │   ├── ChangePasswordEndpoint.cs    # POST /api/auth/change-password
│   │   ├── ForgotPasswordEndpoint.cs    # POST /api/auth/forgot-password
│   │   ├── ResetPasswordEndpoint.cs     # POST /api/auth/reset-password
│   │   └── ConfirmEmailEndpoint.cs      # GET  /api/auth/confirm-email
│   └── Permissions/
│       ├── GetPermissionsEndpoint.cs     # GET  /api/permissions
│       ├── UpdatePermissionsEndpoint.cs  # PUT  /api/permissions
│       └── GetPermissionsByRoleEndpoint.cs
│
├── DataSeeding/
│   └── IdentityDataSeeder.cs           # Seed admin role, default permissions
│
├── Extensions/
│   ├── IdentityServiceExtensions.cs     # AddNacIdentity<TUser>()
│   └── IdentityMiddlewareExtensions.cs
│
├── NacIdentityModule.cs                 # Module registration (NacModule subclass)
└── Nac.Identity.csproj
```

**Thiết kế Tenant-Aware:**

```csharp
// Base User — consumer kế thừa class này
public class NacIdentityUser : IdentityUser<Guid>, ITenantEntity, IAuditable
{
    public string TenantId { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public bool IsDeleted { get; set; }
}

// Cùng email khác tenant = khác user
public class TenantAwareUserManager<TUser> : UserManager<TUser>
    where TUser : NacIdentityUser
{
    private readonly ITenantContext _tenantContext;

    public override async Task<TUser?> FindByEmailAsync(string email)
    {
        return Users.FirstOrDefault(u =>
            u.Email == email &&
            u.TenantId == _tenantContext.CurrentTenant.Id);
    }

    public override async Task<IdentityResult> CreateAsync(TUser user, string password)
    {
        user.TenantId = _tenantContext.CurrentTenant.Id;
        return await base.CreateAsync(user, password);
    }
}

// JWT luôn chứa tenant_id
public class JwtTokenService<TUser> where TUser : NacIdentityUser
{
    public string GenerateAccessToken(TUser user, IList<string> roles)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim("tenant_id", user.TenantId),
            // role claims...
        };
        // Sign JWT, return token string
    }
}

// Implements IIdentityService từ Nac.Core
internal class IdentityService<TUser> : IIdentityService
    where TUser : NacIdentityUser
{
    private readonly UserManager<TUser> _userManager;

    public async Task<UserInfo?> GetUserInfoAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new UserInfo(user.Id, user.Email!, null, user.TenantId, roles.ToList());
    }
}
```

**Permission Grant Model:**

```csharp
// Permission grant entity — persisted in identity schema
public class PermissionGrant : Entity<Guid>, ITenantEntity
{
    public string PermissionName { get; set; } = default!;
    public string ProviderName { get; set; } = default!;   // "Role", "User", "Client"
    public string ProviderKey { get; set; } = default!;    // role name, user id, client id
    public string TenantId { get; set; } = default!;
}

// Permission checker — resolves grants from DB, caches per request
internal class PermissionChecker : IPermissionChecker
{
    private readonly IPermissionStore _store;
    private readonly ICurrentUser _currentUser;

    public async Task<bool> IsGrantedAsync(string permissionName)
    {
        // Check user grants → role grants → fallback deny
        var userId = _currentUser.Id;
        var roles = _currentUser.Roles;

        if (await _store.IsGrantedAsync(permissionName, "User", userId.ToString()))
            return true;

        foreach (var role in roles)
        {
            if (await _store.IsGrantedAsync(permissionName, "Role", role))
                return true;
        }

        return false;
    }
}
```

**Database Schema:** `identity`

```
PostgreSQL Database
├── identity schema    → NacIdentityDbContext
│   ├── identity.users
│   ├── identity.roles
│   ├── identity.user_roles
│   ├── identity.user_claims
│   ├── identity.user_tokens
│   ├── identity.refresh_tokens
│   ├── identity.permission_grants     ← MỚI
│   └── __EFMigrationsHistory
```

Tất cả identity tables đều có RLS policy theo `tenant_id`.

### 4.8 Nac.Observability (L2)

**Mục đích:** Observability stack.

Chứa:
- OpenTelemetry config (traces + metrics → Grafana Tempo / Prometheus)
- Serilog enrichers (tenant, correlation)
- Health checks (`/health/live`, `/health/ready`)

### 4.9 Nac.Jobs (L2)

**Mục đích:** Background jobs, tenant-aware.

Chứa:
- Hangfire + PostgreSQL storage
- Tenant-aware job execution
- Retry policies
- Dashboard with permission integration

### 4.10 Nac.Testing (L2)

**Mục đích:** Test infrastructure.

Chứa:
- TestContainers setup (PostgreSQL)
- EF Core InMemory helpers
- `FakeTimeProvider`, `FakeTenantContext`, `FakeCurrentUser`
- `TenantFixture` — test data per tenant
- ArchTest helpers for module boundary enforcement

### 4.11 Nac.WebApi (L3)

**Mục đích:** Composition root — wire mọi thứ lại.

Chứa:
- `AddNacFramework(config)` — Single extension method đăng ký tất cả services
- `UseNacFramework()` — Middleware pipeline (tenant resolve → auth → logging)
- **Module graph resolution** — `[DependsOn]` topo-sort, 5-phase lifecycle execution
- **Convention-based auto-registration** — Scan module assemblies, register by markers + patterns
- **Data seeder orchestration** — Collect `IDataSeeder` implementations, run in topo-sort order
- OpenAPI 3.1 + Scalar UI config
- Global exception → ProblemDetails mapping
- API versioning setup
- CORS, rate limiting, health checks config

---

## 5. Module Lifecycle System

### 5.1 Lifecycle Phases

Framework thực thi 5 phases theo thứ tự dependency (topo-sort):

```
Phase 1: PreConfigureServices   — Configure options of other modules
Phase 2: ConfigureServices      — Register DI services
Phase 3: PostConfigureServices  — Validate, adjust after all registered
Phase 4: OnApplicationInitialization — Middleware, seed data, warmup
Phase 5: OnApplicationShutdown  — Cleanup
```

### 5.2 Dependency Resolution

```csharp
[DependsOn(
    typeof(NacPersistenceModule),
    typeof(NacEventBusModule),
    typeof(NacCachingModule)
)]
public class OrdersModule : NacModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        // Configure options trước khi các module khác register
        context.Services.Configure<NacCachingOptions>(opt =>
        {
            opt.DefaultTTL = TimeSpan.FromMinutes(5);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Explicit: chỉ DbContext cần manual register
        context.Services.AddDbContext<OrdersDbContext>(opt =>
            opt.UseNpgsql(context.Configuration.GetConnectionString("Default")));

        // Tất cả handlers, validators, repos, endpoints
        // được auto-register bởi convention scanner
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        // Map endpoints sẽ tự động từ IEndpoint scan
        // Data seeders sẽ tự động từ IDataSeeder scan
    }
}
```

### 5.3 Module Graph Resolution (trong Nac.WebApi)

```csharp
// Nac.WebApi resolves topo-sort order:
// 1. Scan entry assembly cho [DependsOn] attributes
// 2. Build dependency graph
// 3. Topo-sort → xác định thứ tự khởi tạo
// 4. Execute phases theo thứ tự

// Ví dụ resolution:
// NacCoreModule (no deps)         → Phase 1-3 đầu tiên
// NacPersistenceModule (Core)     → Phase 1-3 tiếp
// NacIdentityModule (Core, Pers.) → Phase 1-3 tiếp
// OrdersModule (Core, Pers., Cache) → Phase 1-3 cuối
//
// Sau đó Phase 4 (OnApplicationInitialization) theo cùng thứ tự
```

---

## 6. Convention-based Auto-Registration

### 6.1 Auto-Registered Types

Khi module load, framework scan assembly và tự động register:

| Pattern | Lifetime | Ghi chú |
|---|---|---|
| `ITransientDependency` | Transient | Service interfaces auto-detected |
| `IScopedDependency` | Scoped | Service interfaces auto-detected |
| `ISingletonDependency` | Singleton | Service interfaces auto-detected |
| `ICommandHandler<,>` | Scoped | Registered in Dispatcher |
| `IQueryHandler<,>` | Scoped | Registered in Dispatcher |
| `IntegrationEventHandler<>` | Scoped | Registered in EventBus |
| `IValidator<>` | Scoped | Registered in ValidationBehavior |
| `IEndpoint` | — | Collected for `MapEndpoints()` |
| `IPermissionDefinitionProvider` | Singleton | Collected at startup |
| `IDataSeeder` | Scoped | Called during initialization |

### 6.2 Interface Auto-Detection

Khi class implement marker interface, framework auto-detect implemented interfaces:

```csharp
// Framework detects OrderRepository implements IOrderRepository
// Registers: services.AddScoped<IOrderRepository, OrderRepository>()
public class OrderRepository : IOrderRepository, IScopedDependency
{
    // ...
}
```

### 6.3 Service Override

Consumer override framework service bằng `[Dependency]` attribute:

```csharp
// Framework registers: IIdentityService → IdentityService<TUser>
// Consumer overrides:
[Dependency(ReplaceServices = true)]
public class CustomIdentityService : IIdentityService, IScopedDependency
{
    // This replaces framework's default implementation
}
```

### 6.4 Explicit Registration

Chỉ cần cho:
- `DbContext` (cần connection string config)
- `IOptions<T>` configuration
- 3rd party services
- Special lifetime overrides

---

## 7. Template System

### 7.1 nac-solution

```bash
dotnet new nac-solution -n MyProject --tenant-mode shared
```

Tạo ra:
- Solution structure với Host + BuildingBlocks
- Docker Compose (PostgreSQL, Redis, RabbitMQ)
- CI/CD pipeline template
- AI rules (CLAUDE.md, .cursor/rules/, copilot-instructions.md)
- .editorconfig, Directory.Build.props
- README với quickstart guide

### 7.2 nac-module

```bash
dotnet new nac-module -n Orders
```

Tạo ra 2 projects:
- `Orders.Contracts/` — Public DTOs, integration events
- `Orders/` — Domain (folder), Features (vertical slices), Infrastructure (folder), module registration

Layer boundaries enforce bằng Architecture Tests (namespace-level). Khi module quá lớn → tách thành 2 modules nhỏ hơn.

### 7.3 nac-entity, nac-endpoint

```bash
dotnet new nac-entity -n Product --module Catalog
dotnet new nac-endpoint -n GetProducts --module Catalog --type query
```

Scaffold individual files theo convention.

---

## 8. AI Rules Strategy

### Cấu trúc

```
ai-rules/
├── conventions.md            # Source of truth — tool-agnostic
├── CLAUDE.md                 # Claude Code adapter
├── .cursor/rules/
│   ├── nac-general.mdc       # General conventions
│   ├── nac-cqrs.mdc          # CQRS patterns
│   ├── nac-entity.mdc        # Entity/aggregate patterns
│   └── nac-endpoint.mdc      # API endpoint patterns
├── copilot-instructions.md   # GitHub Copilot adapter
└── AGENTS.md                 # Generic agents
```

### Nguyên tắc viết rules

- **Cụ thể, không chung chung**: "All service methods return `Result<T>` from Nac.Core" thay vì "write clean code"
- **Có ví dụ code**: Mỗi convention kèm ít nhất 1 code example
- **Iterate liên tục**: Mỗi lần sửa AI output → thêm rule mới
- Templates tự copy ai-rules vào consumer project khi scaffold

---

## 9. CI/CD Pipeline

```
Push to main
    │
    ├── Build all packages
    ├── Run unit tests
    ├── Run architecture tests (NetArchTest)
    ├── Run integration tests (TestContainers + PostgreSQL)
    │
    ├── [Tag v*] Pack NuGet packages
    ├── [Tag v*] Pack templates
    └── [Tag v*] Push to private NuGet feed
```

### Versioning

- Semantic versioning: MAJOR.MINOR.PATCH
- Tất cả packages cùng version (coordinated release)
- `Directory.Build.props` chứa version chung
- Breaking changes → bump MAJOR

---

## 10. Quyết định kỹ thuật

| Quyết định | Lựa chọn | Lý do |
|---|---|---|
| CQRS dispatcher | Tự viết (FrozenDictionary) | Full control, AI-friendly, zero license risk |
| Messaging | Wolverine (MIT) | Thay MediatR + MassTransit (đã commercial) |
| Multi-tenancy | Finbuckle + PostgreSQL RLS | Defense-in-depth, .NET Foundation |
| ORM | EF Core 10 + Npgsql | Named query filters, vector search |
| Identity | ASP.NET Core Identity + custom JWT | Tenant-aware, full control, no external IdP dependency |
| Permissions | Custom permission grid | ABP-inspired, per-tenant grants, User/Role/Client providers |
| Caching | HybridCache | L1+L2, stampede protection, tag invalidation |
| API docs | OpenAPI 3.1 + Scalar | Built-in .NET 10, thay Swashbuckle |
| Logging | Serilog → OpenTelemetry | Structured logs + trace correlation |
| Background jobs | Hangfire + PostgreSQL | Dashboard, tenant-aware |
| API gateway | YARP | Microsoft-backed, config-driven |
| Keycloak | Optional SSO (trong Nac.Identity) | Multi-tenant SSO, Organizations ext |
| Arch testing | NetArchTest | Module boundary enforcement |
| Module lifecycle | 5-phase + [DependsOn] topo-sort | ABP-inspired, cross-module configuration |
| DI registration | Convention-based auto-registration | Markers + known patterns, zero boilerplate |
| Object mapping | Manual (no library) | Framework documents, consumer decides |
| Data seeding | IDataSeeder interface | Auto-collected, run at startup |
| Service override | [Dependency(ReplaceServices)] | Consumer override framework impls |

---

## 11. Nguyên tắc thiết kế

1. **Convention within modules, explicit across modules** — Module tự auto-scan assembly riêng (handlers, validators, repos). Cross-module refs vẫn explicit qua Contracts. `[DependsOn]` khai báo rõ dependency graph.
2. **Package independence** — Consumer có thể cherry-pick packages. Nac.Core đủ dùng standalone.
3. **Zero reflection sau startup** — Dispatcher cache delegates. Convention scanner chạy 1 lần. Source generators ưu tiên.
4. **PostgreSQL-first** — Single infra dependency cho data, messaging, jobs, cache coordination.
5. **AI-native** — Strong typing, consistent naming, canonical examples, instruction files.
6. **Upgrade-safe** — Consumer reference NuGet, không copy source. Upgrade = bump version.
7. **Interface lên Core, implementation ở L2** — Abstractions (IIdentityService, IPermissionChecker, ICurrentUser, event contracts) nằm ở Nac.Core (L0). Implementation nằm ở L2 packages. Business modules chỉ reference Nac.Core.
