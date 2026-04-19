# NAC Framework — Advanced Layers (L2)

Detailed documentation for EventBus, Testing, MultiTenancy, and Identity layers.

## Contents

- [EventBus Layer](#eventbus-layer)
- [Testing Layer](#testing-layer)
- [MultiTenancy Layer](#multitenancy-layer)
- [Identity Layer](#identity-layer)

---

## EventBus Layer

### IEventPublisher
**Purpose:** Publish integration events to the bus

**Contract:**
```csharp
public interface IEventPublisher
{
    Task PublishAsync(IIntegrationEvent @event, CancellationToken ct = default);
    Task PublishAsync(IEnumerable<IIntegrationEvent> events, CancellationToken ct = default);
}
```

### IEventHandler<TEvent>
**Purpose:** Handle a specific integration event type

**Contract:**
```csharp
public interface IEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
```

### IEventDispatcher
**Purpose:** Route events to all registered handlers for that event type

**Dispatch Strategy:**
- FrozenDictionary<Type, FrozenSet<Type>> registry for O(1) lookup
- Fan-out execution: all handlers invoked (errors logged, not rethrown)
- Assembly scanning for automatic handler discovery

### InMemoryEventBus
**Transport:** System.Threading.Channels (bounded, 1000 capacity)

**Characteristics:**
- Lock-free channel-based publishing
- Background worker (InMemoryEventBusWorker) processes events
- Suitable for single-process deployments
- Fail-safe: one failing handler doesn't block others

### OutboxEventPublisher
**Purpose:** Bridge Persistence Outbox to EventBus

**Role:**
- Implements IIntegrationEventPublisher (from Persistence layer)
- Deserializes outbox payloads (string eventType + JSON)
- Routes to IEventPublisher for in-memory or external transport
- Allowlist-based event type validation

### EventHandlerRegistry
**Features:**
- Assembly scanning via reflection
- Handler discovery: `IEventHandler<T>` implementations
- Multi-handler support (fan-out pattern)
- Thread-safe registration

### Usage Pattern
```csharp
// 1. Publish event (synchronous to bus)
await _eventPublisher.PublishAsync(new UserRegisteredEvent(...));

// 2. Background worker processes asynchronously
// InMemoryEventBusWorker reads from channel

// 3. Dispatcher routes to all handlers
// All UserRegisteredEvent handlers invoked in parallel

// 4. Handlers execute independently
public class SendWelcomeEmailHandler : IEventHandler<UserRegisteredEvent>
{
    public async Task HandleAsync(UserRegisteredEvent @event, CancellationToken ct)
    {
        await _emailService.SendWelcomeAsync(@event.Email, ct);
    }
}
```

---

## Testing Layer

### Fakes (7 In-Memory Implementations)

**FakeCurrentUser** — ICurrentUser implementation
- Settable Id, Name, IsAuthenticated
- Mutable Roles collection
- No external dependencies

**FakeDateTimeProvider** — IDateTimeProvider implementation
- Settable UtcNow property
- Deterministic time for reproducible tests

**FakePermissionChecker** — IPermissionChecker implementation
- GrantAll() — grant all permissions
- DenyAll() — deny all permissions
- Custom grant/deny lists

**FakeRepository<T>** — IRepository<T> implementation
- In-memory item storage (List<T>)
- Tracks Add/Update/Delete operations
- Supports Specification<T> filtering
- WithItems() fluent seed method

**FakeEventPublisher** — IEventPublisher implementation
- Collects published events (PublishedEvents list)
- No actual dispatch (testing only)

**FakeSender** — ISender implementation (CQRS)
- Collects sent commands/queries (SentRequests list)
- Optional result overrides

**FakeNacCache** — INacCache implementation
- In-memory cache store
- Tag-based invalidation tracking

### Builders

**TestEntityBuilder<TEntity, TBuilder>** — Abstract fluent builder
- Generic entity creation via reflection
- WithProperty(name, value) fluent API
- Customizable CreateEntity() for special construction

**ResultBuilder** — Fluent builder for Result<T>
- Success(value) — create success result
- Failure(status, errors) — create failure result

### Fixtures

**NacTestFixture** — Pre-configured DI container
- All 7 fakes pre-registered
- Override ConfigureServices() to add custom services
- GetService<T>() for dependency retrieval
- Implements IDisposable for cleanup

**InMemoryDbContextFixture<TContext>** — EF Core in-memory DB
- Creates isolated in-memory databases (unique GUID names)
- CreateContext() — factory for fresh DbContext instances
- Suitable for integration tests with same DB state

### Assertion Extensions

**ResultAssertionExtensions** — FluentAssertions integration
- Should().BeSuccess() — assert success status
- Should().BeFailed() — assert failure
- Should().HaveStatus(status) — specific status check
- Error message assertions

### AddNacTesting() Extension
Registers all fakes in IServiceCollection:
```csharp
services.AddNacTesting();
// Automatically registers all fakes with appropriate lifetimes
```

---

## MultiTenancy Layer

### ITenantContext
**Purpose:** Access and manage current tenant context within request scope

**Contract:**
```csharp
public interface ITenantContext
{
    TenantInfo? Current { get; }
    string? TenantId => Current?.Id;
    void SetCurrentTenant(TenantInfo? tenant);
}
```

### TenantInfo
**Purpose:** Immutable tenant metadata holder

**Properties:**
- `Id`: Tenant unique identifier
- `Name`: Display name
- `Metadata`: Custom tenant attributes (Dictionary<string, object>)

### ITenantStore
**Purpose:** Persist and retrieve tenant metadata

**Contract:**
```csharp
public interface ITenantStore
{
    Task<TenantInfo?> GetAsync(string tenantId);
    Task<TenantInfo?> GetByNameAsync(string tenantName);
    Task AddAsync(TenantInfo tenant);
    Task UpdateAsync(TenantInfo tenant);
    Task RemoveAsync(string tenantId);
}
```

### Tenant Resolution Strategies (4 Built-In)

**HeaderTenantStrategy** — Extract from HTTP header
```csharp
// Resolves from X-Tenant-Id header (configurable)
// Usage: AddNacMultiTenancy().AddHeaderTenantStrategy("X-Tenant-Id")
```

**ClaimTenantStrategy** — Extract from JWT claim
```csharp
// Resolves from JWT claim (default: "tenant_id")
// Usage: AddNacMultiTenancy().AddClaimTenantStrategy("tenant_id")
```

**RouteTenantStrategy** — Extract from route parameter
```csharp
// Resolves from route parameter (default: "{tenantId}")
// Usage: AddNacMultiTenancy().AddRouteTenantStrategy("tenantId")
```

**SubdomainTenantStrategy** — Extract from subdomain
```csharp
// Resolves from subdomain (e.g., acme.app.com → "acme")
// Usage: AddNacMultiTenancy().AddSubdomainTenantStrategy()
```

### TenantResolutionMiddleware
**Purpose:** Resolve tenant from request and set in ITenantContext

**Processing:**
1. Run registered strategies in order
2. Set ITenantContext.Current on first match
3. Store in HttpContext.Items for downline access
4. Pass to next middleware

### MultiTenantDbContext
**Purpose:** EF Core base with automatic RLS (Row-Level Security) filters

**Features:**
- Auto-adds query filter: `entity.TenantId == context.CurrentTenantId`
- Stacks with soft-delete filters (composable)
- Per-entity opt-in via `HasQueryFilter()`
- Multi-tenancy scoped to IUnitOfWork

**Usage:**
```csharp
public class MyDbContext : MultiTenantDbContext
{
    public DbSet<Customer> Customers { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Auto-filters Customer by TenantId in all queries
    }
}
```

### TenantEntityInterceptor
**Purpose:** EF Core SaveChanges interceptor to auto-set TenantId

**Behavior:**
- Before persisting entity, check ITenantEntity interface
- If `TenantId` is unset, copy from ITenantContext.Current
- Silent no-op if tenant is null (optional validation)

### ITenantConnectionStringResolver
**Purpose:** Per-tenant database isolation

**Contract:**
```csharp
public interface ITenantConnectionStringResolver
{
    string Resolve(string tenantId);
}
```

**Pattern:** Register custom implementation for sharded/isolated DB architectures

### AddNacMultiTenancy() Extension
**Registers:**
- ITenantContext (scoped)
- ITenantStore (InMemory by default; override for persistence)
- Strategy resolvers (chain pattern)
- TenantResolutionMiddleware
- TenantEntityInterceptor
- NacMultiTenancyModule

---

## Identity Layer

### NacUser
**Purpose:** Application user extending ASP.NET Core Identity

**Extends:** `IdentityUser<Guid>`

**Additional Properties:**
- `TenantId`: Multi-tenancy support
- `FullName`: Display name
- `IsActive`: Account activation flag
- `CreatedAt`, `UpdatedAt`, `CreatedBy`: Audit trail (IAuditableEntity)
- `IsDeleted`, `DeletedAt`: Soft-delete (ISoftDeletable)

### NacRole
**Purpose:** Application role extending ASP.NET Core Identity

**Extends:** `IdentityRole<Guid>`

**Supports:** Standard role-based authorization

### NacIdentityDbContext
**Purpose:** EF Core DbContext for identity and application entities

**Generics:** `NacIdentityDbContext<TContext>` for custom app entities

**Includes:**
- ASP.NET Core Identity tables (Users, Roles, UserClaims, etc.)
- Multi-tenancy support (TenantId on users)
- Audit and soft-delete tracking

### CurrentUserAccessor
**Purpose:** Extract ICurrentUser from JWT claims

**Implementation:**
- Reads claim set from HttpContext.User
- Populates ICurrentUser with Id, Name, IsAuthenticated, Roles
- Registered as scoped; null-safe for unauthenticated requests

**Usage:**
```csharp
var currentUser = context.ServiceProvider.GetRequiredService<ICurrentUser>();
var userId = currentUser.Id; // From JWT subject claim
var roles = currentUser.Roles; // From "role" claims
```

### IdentityService
**Purpose:** Wrapper around UserManager for user queries

**Methods:**
- `GetUserInfoAsync(Guid userId)` → UserInfo with roles
- `GetUsersAsync(IEnumerable<Guid> userIds)` → Bulk user info
- `IsInRoleAsync(Guid userId, string role)` → Role check

### JwtTokenService
**Purpose:** Issue JWT tokens with configurable claims

**Features:**
- Configurable secret, issuer, audience
- Auto-includes subject (UserId), roles, tenant
- Expiration via JwtOptions
- HMAC SHA-256 signature

**Configuration (JwtOptions):**
```csharp
services.AddNacIdentity<MyContext>()
    .ConfigureJwt(options =>
    {
        options.Secret = config["Jwt:Secret"];
        options.Issuer = config["Jwt:Issuer"];
        options.Audience = config["Jwt:Audience"];
        options.ExpiryMinutes = 60;
    });
```

### PermissionDefinitionManager
**Purpose:** Registry of all application permissions (FrozenDictionary)

**Features:**
- Hierarchical permission groups
- IsGrantedByDefault flag
- Immutable after registration
- Thread-safe access

**Usage:**
```csharp
public class MyPermissions : IPermissionDefinitionProvider
{
    public void Define(IPermissionDefinitionContext context)
    {
        var group = context.AddGroup("UserManagement");
        group.AddPermission("Create", isGrantedByDefault: false);
        group.AddPermission("Delete", isGrantedByDefault: false);
    }
}
```

### PermissionChecker
**Purpose:** Check if user has permission via claims + hierarchical rules

**Decision Logic:**
1. Check JWT claims (role-based)
2. Check hierarchical defaults (parent → child)
3. Return boolean

**Usage:**
```csharp
if (await _permissionChecker.IsGrantedAsync("Users.Create"))
{
    // User has permission
}
```

### PermissionAuthorizationHandler
**Purpose:** ASP.NET Core Authorization handler for permission gates

**Requirement:** `PermissionRequirement(string permissionName)`

**Usage (Controllers):**
```csharp
[Authorize(Policy = "Users.Delete")]
public async Task<IActionResult> DeleteUser(Guid id)
{
    // Only users with "Users.Delete" permission
}
```

### AddNacIdentity<TContext>() Extension
**Registers:**
- NacUser + NacRole via Identity
- UserManager<NacUser>, RoleManager<NacRole>
- IdentityService (IIdentityService)
- CurrentUserAccessor (ICurrentUser)
- JwtTokenService
- PermissionDefinitionManager
- PermissionChecker (IPermissionChecker)
- PermissionAuthorizationHandler
- NacIdentityModule

---

**Last Updated:** 2026-04-16 (Wave 2B)
**Target Framework:** .NET 10.0 LTS
