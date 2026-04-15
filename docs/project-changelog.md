# NAC Framework — Project Changelog

Detailed record of significant changes, features, and fixes across all versions.

---

## [2.0.0] — April 2026

### Module Architecture Redesign (Phase 4)

**Breaking Change:** Module pattern changed from 2-project split (`Module.Core` + `Module.Infrastructure`) to a single-project with clean architecture enforced by folder structure.

**What changed:**
- Each module is now one `.csproj` with `Domain/`, `Application/`, `Contracts/`, `Infrastructure/`, and `Endpoints/` folders
- `CatalogModule : INacModule` exposes only `Name` + `ConfigureServices` — no `ConfigureEndpoints` or `ConfigurePipeline`
- `IEndpointMapper` implementations are auto-discovered by `UseNacFramework()` — no manual registration
- CLI scaffolding (`nac new`) generates the single-project layout with all infrastructure files in `Infrastructure/` subfolder

**Rationale:** Reduces scaffolding complexity, enforces KISS, and keeps module boundaries via folders rather than project references.

**Migration from 1.x:**
- Merge `{Module}.Infrastructure` project files into `{Module}` project under `Infrastructure/` folder
- Remove `ConfigureEndpoints` and `ConfigurePipeline` from module class — implement `IEndpointMapper` separately
- Update solution file (`.slnx`) to remove the `.Infrastructure` project reference

---

## [1.0.2] — April 2026

### Identity Enhancements

**New APIs:**
- `IIdentityService` in `Nac.Core.Auth` — query user info from business modules without coupling to `Nac.Identity`
- `UserInfo` record — lightweight user identity DTO
- `IdentityEventPublisher` — publish identity lifecycle events (registration, confirmation, reset)
- `UserRegisteredEvent`, `UserEmailConfirmedEvent`, `PasswordResetEvent` integration events

**Bug Fixes:**
- `RefreshToken` now stores `TenantId`; preserved on token refresh to maintain tenant context

**Breaking Changes:**
- `UseNacIdentity()` no longer accepts `seedRoles` parameter. Call `IdentitySeeder.SeedDefaultRolesForTenantAsync(tenantId)` when creating new tenants.
- `IdentitySeeder.SeedDefaultRolesAsync()` replaced by `SeedDefaultRolesForTenantAsync(string tenantId)` (per-tenant explicit).

**Architecture:**
- Permission loading moved to `UseNacIdentity()` middleware — avoids sync-over-async penalties; `ICurrentUser.Permissions` is safe to access synchronously in handlers.

---

## [1.0.0] — April 2026

### Initial Release

All 15 packages implemented:

| Package | Version | Notes |
|---------|---------|-------|
| Nac.Core | 1.0.0 | Base types + contracts (Entity, AggregateRoot, ValueObject) |
| Nac.Domain | 1.0.0 | DomainEvent, persistence contracts |
| Nac.CQRS | 1.0.0 | Custom CQRS mediator (renamed from Nac.Mediator) |
| Nac.Persistence | 1.0.0 | EF Core, UnitOfWork, Repository, Outbox |
| Nac.Persistence.PostgreSQL | 1.0.0 | PostgreSQL provider |
| Nac.Messaging | 1.0.0 | IEventBus, InMemoryEventBus, Outbox |
| Nac.Messaging.RabbitMQ | 1.0.0 | RabbitMQ integration |
| Nac.MultiTenancy | 1.0.0 | 3 strategies, 5 resolvers |
| Nac.Caching | 1.0.0 | Query-level caching + invalidation |
| Nac.Observability | 1.0.0 | Structured logging behaviors |
| Nac.WebApi | 1.0.0 | Response envelopes, exception handler, module framework |
| Nac.Identity | 1.0.0 | ASP.NET Identity + JWT + tenant-scoped roles |
| Nac.Testing | 1.0.0 | FakeEventBus, FakeTenantContext, FakeCurrentUser |
| Nac.Cli | 1.0.0 | `nac new` CLI tool |
| Nac.Templates | 1.0.0 | `dotnet new nac-solution` |

**Package Restructure (from pre-release):**

| Change | Before | After |
|--------|--------|-------|
| Abstractions package | `Nac.Abstractions` | Deleted — types distributed |
| CQRS package | `Nac.Mediator` | `Nac.CQRS` (namespace `Nac.CQRS.*`) |
| Base domain types | `Nac.Domain` | Moved to `Nac.Core` |
| Repo interfaces | `Nac.Core` | Moved to `Nac.Domain.Persistence` |
| `ICommand`, `IQuery` | `Nac.Core.Messaging` | `Nac.CQRS.Abstractions` |
| Module framework | `Nac.Abstractions.Modularity` | `Nac.WebApi.Modularity` |
| Identity user class | `NacUser` (sealed) | `NacIdentityUser` (unsealed) |
| Identity DbContext | `NacIdentityDbContext` | `NacIdentityDbContext<TUser>` (generic) |

---

## Versioning Policy

- **v1.x:** API stable; breaking changes deferred to v2
- **v2+:** Semantic versioning enforced
- **Deprecation window:** 6 months before breaking change takes effect
- **Security patches:** Same-day (critical), weekly (high), monthly (medium)
