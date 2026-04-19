# NAC Framework — Codebase Summary

**Framework:** .NET 10 LTS | **License:** MIT | **Architecture:** Mono-repo NuGet package collection

## Overview

NAC Framework is a foundational DDD-based framework published as a suite of layered NuGet packages. The mono-repo contains source code, tests, templates, examples, and documentation. Consumers integrate via NuGet—never copying source.

**Current Status:** L0 Nac.Core + Wave 1 (L1 Nac.Cqrs, Nac.Caching + L2 Nac.Persistence) + Wave 2A (Nac.EventBus, Nac.Testing) + Wave 2B (Nac.Identity, Nac.MultiTenancy) + Wave 2C (Nac.Observability, Nac.Jobs) + Wave 3 (L3 Nac.WebApi) fully implemented and tested (626 framework unit tests + 11 consumer integration tests passing). Consumer reference blueprint shipped (samples/ReferenceApp). Pattern A Identity Migration (Phases 01–07) complete.

## Package Layers

### L0 — Core (Zero External Dependencies)
**Package:** `Nac.Core` | **Status:** Complete | **Tests:** 190 passing

Foundation layer with DDD primitives, modularity, and result patterns.

**Modules:**
- **Primitives:** Entity, AggregateRoot, ValueObject, StronglyTypedId, DomainEvent
- **Results:** Result, Result<T>, ResultStatus, ValidationError
- **Domain:** IRepository, IReadRepository, Specification (boolean logic), Guard, DomainError
- **Modularity:** NacModule, DependsOn, ServiceConfigurationContext, ApplicationInitializationContext/Shutdown
- **DI:** ITransientDependency, IScopedDependency, ISingletonDependency, DependencyAttribute
- **Abstractions:**
  - Identity: ICurrentUser, IIdentityService, UserInfo
  - Permissions: PermissionDefinition, PermissionGroup, IPermissionChecker, IPermissionDefinitionProvider
  - Events: IIntegrationEvent + UserRegistered, UserEmailConfirmed, PasswordReset events
  - IDateTimeProvider
- **Data Seeding:** IDataSeeder, DataSeedContext
- **Value Objects:** Money, Address, DateRange, Pagination

### L1 — Higher-Order Abstractions (Wave 1 COMPLETE)
**Tests:** 65 new tests (all passing)

- **Nac.Cqrs:** Custom CQRS dispatcher with FrozenDictionary O(1) lookup, sealed handler pattern, ValueTask<T> returns, 4 pipeline behaviors (Validation, Logging, Caching, Transaction), ISender interface, assembly scanning
- **Nac.Caching:** INacCache abstraction over HybridCache (.NET 10+), tenant-aware key prefixing, tag-based invalidation, CacheKey utility

### L2 — Feature Layers (Wave 1 COMPLETE)
**Tests:** 65 tests (all passing)

- **Nac.Persistence:** NacDbContext abstract base, Repository<T> generic impl, Specification-to-IQueryable bridge, 4 EF Core interceptors (AuditableEntity, SoftDelete, DomainEvent, Outbox), OutboxWorker BackgroundService

### L2 — Feature Layers (Wave 2A COMPLETE)
**Tests:** 80+ new tests (all passing)

- **Nac.EventBus:** IEventPublisher/IEventHandler<T>/IEventDispatcher interfaces, InMemory transport (System.Threading.Channels, 1000 capacity), Outbox bridge (IIntegrationEventPublisher impl), assembly scanning handler registry (FrozenDictionary, fan-out dispatch), NacEventBusModule
- **Nac.Testing:** 7 in-memory fakes (FakeCurrentUser, FakeDateTimeProvider, FakePermissionChecker, FakeRepository<T>, FakeEventPublisher, FakeSender, FakeNacCache), TestEntityBuilder<T,B> abstract fluent builder + ResultBuilder, NacTestFixture (DI with fakes), InMemoryDbContextFixture<T> for EF Core, ResultAssertionExtensions (FluentAssertions), AddNacTesting() extension

### L2 — Feature Layers (Wave 2B COMPLETE)
**Tests:** 150 new tests (all passing) = 61 MultiTenancy + 89 Identity

- **Nac.MultiTenancy:** TenantInfo/ITenantContext/ITenantStore abstractions, 4 resolution strategies (Header, Claim, Route, Subdomain), TenantResolutionMiddleware, MultiTenantDbContext with RLS query filters, TenantEntityInterceptor, ITenantConnectionStringResolver, AddNacMultiTenancy() + UseNacMultiTenancy() extensions
- **Nac.Identity (Phase 01–04):** NacUser (global, no TenantId), NacRole (tenant-scoped + templates), UserTenantMembership (M:N), MembershipRole, PermissionGrant (ABP-style), NacIdentityDbContext, JwtTokenService, PermissionChecker with cache, IRoleService with clone, RoleTemplateSystem, AuthEndpoints (7 routes), AddNacIdentity<TContext>() extension

### L2 — Feature Layers (Pattern A Phase 05–07 COMPLETE)
**Tests:** 11 new tests (all passing); 626 total framework tests

- **Nac.Identity (Phase 05–07 Enhancements):** 
  - IMembershipService/MembershipService: Invite, Accept, List, ChangeRoles (cache invalidation), CreateActiveMembershipAsync
  - ITenantSwitchService/TenantSwitchService: Active membership validation, tenant-scoped JWT re-issue
  - HostPermissions.cs constant: `Host.AccessAllTenants`
  - HostPermissionProvider.cs: IPermissionDefinitionProvider for host realm
  - HostQueryExtensions.cs: `AsHostQueryAsync<T>` bypasses tenant filter for host users
  - TenantRequiredGateMiddleware: Auto-registered in UseNacApplication (after auth, before authz); gates tenant-scoped endpoints
  - HostAdminOnlyFilter: Checks `IsHost` flag AND `Host.AccessAllTenants` permission grant
  - ForbiddenAccessException.cs (Nac.Core/Domain): Maps to HTTP 403 in NacExceptionHandler
  - JWT shape: `sub, email, name?, tenant_id?, role_ids?, is_host?`
  - Pattern A finalized: Global users, runtime permission resolution (cache→DB)

### L2 — Feature Layers (Wave 2B-Enhancement COMPLETE)
**Tests:** 38 new tests (all passing)

- **Nac.MultiTenancy.Management:** Tenant aggregate (AggregateRoot<Guid> with audit/soft-delete), 5 domain events (Created, Updated, Deleted, Activated, Deactivated), TenantManagementDbContext registry DB, EfCoreTenantStore with 10-min sliding cache, EncryptedConnectionStringResolver (Microsoft.AspNetCore.DataProtection), 11 REST endpoints (/api/admin/tenants), HostAdminOnlyFilter authorization, bulk operations (activate/deactivate/delete), outbox-emitted domain events, AddNacTenantManagement() extension

### L2 — Feature Layers (Wave 2C COMPLETE)
**Tests:** 49 new tests (all passing) = 28 Observability + 21 Jobs

- **Nac.Observability:** LoggingEnricherMiddleware (structured logging enrichment), diagnostic name constants (NacActivitySources, NacMeters), NacLoggingScope extension for scoped logging context
- **Nac.Jobs:** Pure abstractions — IJobScheduler, IRecurringJobManager, IJobHandler<T> interfaces + JobDefinition metadata class, FakeJobScheduler and FakeRecurringJobManager fakes for testing

### L3 — Composition Root (Wave 3 COMPLETE)
**Package:** `Nac.WebApi` | **Status:** Complete | **Tests:** 42 passing

Composition root layer integrating all L0-L2 packages with web infrastructure.

**Components:**
- **NacWebApiModule:** DependsOn NacCoreModule + NacObservabilityModule
- **NacWebApiOptions:** Toggles for EnableApiVersioning, EnableOpenApi, EnableCors, EnableRateLimiting, EnableResponseCompression, EnableHealthChecks
- **NacApplicationFactory:** Pre/Config/Post lifecycle hooks for module initialization
- **NacApplicationLifetime:** IHostedService for Init/Shutdown
- **NacModuleLoader:** Kahn's algorithm topological sort with cycle detection
- **NacExceptionHandler:** RFC 9457 ProblemDetails global exception handler
- **ResultToHttpMapper:** 6 ResultStatus → HTTP status code mapping
- **UseNacApplication():** Middleware pipeline with 13 ordered stages, conditional middleware
- **AddNacWebApi():** Options configuration entry point

## Codebase Structure

```
NACFramework/
├── src/
│   ├── Nac.Core/                 [L0 - Zero dependencies]
│   │   ├── Primitives/           [Entity, AggregateRoot, ValueObject, etc.]
│   │   ├── Results/              [Result pattern implementation]
│   │   ├── Domain/               [Repository, Specification, Guard, DomainError]
│   │   ├── Modularity/           [NacModule, DependsOn, Contexts]
│   │   ├── DependencyInjection/  [DI marker interfaces]
│   │   ├── Abstractions/         [Identity, Permissions, Events, DateTime]
│   │   ├── DataSeeding/          [IDataSeeder, DataSeedContext]
│   │   └── ValueObjects/         [Money, Address, DateRange, Pagination]
│   ├── Nac.Cqrs/                 [L1 - CQRS implementation]
│   │   ├── Commands/             [ICommand, ICommandHandler<,> sealed handlers]
│   │   ├── Queries/              [IQuery, IQueryHandler<,> sealed handlers]
│   │   ├── Dispatching/          [ISender dispatcher with FrozenDictionary]
│   │   ├── Pipeline/             [Validation, Logging, Caching, Transaction behaviors]
│   │   ├── Markers/              [ICacheableQuery, ITransactionalCommand]
│   │   └── Extensions/           [AddNacCqrs() service extension]
│   ├── Nac.Caching/              [L1 - Caching implementation]
│   │   ├── INacCache.cs          [HybridCache abstraction]
│   │   ├── NacCache.cs           [HybridCache wrapper impl]
│   │   ├── CacheKey.cs           [Static utility for key construction]
│   │   ├── CacheEntryOptions.cs  [Cache configuration]
│   │   └── Extensions/           [AddNacCaching() service extension]
│   ├── Nac.Persistence/          [L2 - EF Core implementation]
│   │   ├── Context/              [NacDbContext abstract base]
│   │   ├── Repository/           [Repository<T> generic implementation]
│   │   ├── Specifications/       [SpecificationExtensions for IQueryable]
│   │   ├── Interceptors/         [AuditableEntity, SoftDelete, DomainEvent]
│   │   ├── Outbox/               [Outbox pattern: OutboxEvent, OutboxWorker]
│   │   └── Extensions/           [AddNacPersistence<T>() service extension]
│   ├── Nac.EventBus/             [L2 - Event bus & pub/sub]
│   │   ├── Abstractions/         [IEventPublisher, IEventHandler<T>, IEventDispatcher]
│   │   ├── InMemory/             [InMemoryEventBus, Channels-based transport]
│   │   ├── Handlers/             [EventDispatcher, EventHandlerRegistry (FrozenDictionary)]
│   │   ├── Outbox/               [OutboxEventPublisher bridge]
│   │   ├── Extensions/           [AddNacEventBus() service extension]
│   │   └── NacEventBusModule/    [Modular registration]
│   ├── Nac.Testing/              [L2 - Testing infrastructure]
│   │   ├── Fakes/                [FakeCurrentUser, FakeDateTimeProvider, FakePermissionChecker, etc.]
│   │   ├── Builders/             [TestEntityBuilder<T,B>, ResultBuilder]
│   │   ├── Fixtures/             [NacTestFixture, InMemoryDbContextFixture<T>]
│   │   ├── Extensions/           [ResultAssertionExtensions, AddNacTesting()]
│   │   └── Properties/           [AssemblyInfo, metadata]
│   ├── Nac.MultiTenancy/         [L2 - Multi-tenancy support]
│   │   ├── Abstractions/         [TenantInfo, ITenantContext, ITenantStore]
│   │   ├── Context/              [TenantContext, InMemoryTenantStore]
│   │   ├── Resolution/           [4 strategies: Header, Claim, Route, Subdomain]
│   │   ├── EfCore/               [MultiTenantDbContext, TenantEntityInterceptor]
│   │   ├── Factory/              [ITenantConnectionStringResolver]
│   │   ├── Extensions/           [AddNacMultiTenancy(), UseNacMultiTenancy()]
│   │   └── NacMultiTenancyModule/[Modular registration]
│   ├── Nac.MultiTenancy.Management/ [L2 - Tenant management & admin API]
│   │   ├── Domain/               [Tenant aggregate, 5 domain events]
│   │   ├── Persistence/          [TenantManagementDbContext, EfCoreTenantStore, EncryptedResolver]
│   │   ├── Services/             [TenantManagementService, TenantMapper]
│   │   ├── Controllers/          [TenantsController - 11 REST endpoints]
│   │   ├── Dtos/                 [CreateTenantRequest, UpdateTenantRequest, TenantResponse, etc.]
│   │   ├── Authorization/        [HostAdminOnlyFilter]
│   │   ├── Validators/           [TenantRequestValidators]
│   │   ├── Extensions/           [AddNacTenantManagement()]
│   │   └── NacTenantManagementModule/ [Modular registration]
│   ├── Nac.Identity/             [L2 - Identity & authentication (Phases 01–07)]
│   │   ├── Users/                [NacUser (global), NacRole (tenant-scoped + template)]
│   │   ├── Memberships/          [UserTenantMembership, MembershipRole, IMembershipService]
│   │   ├── Context/              [NacIdentityDbContext, NacIdentityDbContext<TUser>]
│   │   ├── Services/             [CurrentUserAccessor, IdentityService, TenantSwitchService]
│   │   ├── Jwt/                  [JwtOptions, JwtTokenService, NacIdentityClaims]
│   │   ├── Permissions/          [PermissionDefinitionManager, PermissionChecker, PermissionGrantCache]
│   │   │   ├── Host/             [HostPermissions, HostPermissionProvider (Phase 07)]
│   │   │   ├── Grants/           [PermissionGrant, IPermissionGrantRepository, PermissionProviderNames]
│   │   │   └── Cache/            [IPermissionGrantCache, DistributedPermissionGrantCache]
│   │   ├── Roles/                [IRoleService, RoleTemplateSystem, RoleTemplateSeeder]
│   │   ├── Endpoints/            [AuthEndpoints (7 routes), TenantRequiredGateMiddleware (Phase 07)]
│   │   ├── Persistence/          [HostQueryExtensions (Phase 07)]
│   │   ├── Authorization/        [PermissionAuthorizationHandler, HostAdminOnlyFilter]
│   │   ├── Extensions/           [ServiceCollectionExtensions, NacIdentityOptions]
│   │   └── NacIdentityModule/    [Modular registration]
│   ├── Nac.Observability/        [L2 - Observability & logging]
│   │   ├── Logging/              [LoggingEnricherMiddleware, NacLoggingScope]
│   │   ├── Diagnostics/          [NacActivitySources, NacMeters constants]
│   │   ├── Extensions/           [AddNacObservability() service extension]
│   │   └── NacObservabilityModule/[Modular registration]
│   ├── Nac.Jobs/                 [L2 - Job scheduling abstractions]
│   │   ├── Abstractions/         [IJobScheduler, IRecurringJobManager, IJobHandler<T>]
│   │   ├── Models/               [JobDefinition metadata]
│   │   ├── Fakes/                [FakeJobScheduler, FakeRecurringJobManager for testing]
│   │   ├── Extensions/           [AddNacJobs() service extension]
│   │   └── NacJobsModule/        [Modular registration]
│   └── Nac.WebApi/               [L3 - Composition root]
│       ├── ExceptionHandling/    [NacExceptionHandler, ResultToHttpMapper, RFC 9457]
│       ├── Extensions/           [AddNacWebApi(), UseNacApplication(), middleware pipeline]
│       ├── NacWebApiOptions.cs   [Feature toggles: API versioning, OpenAPI, CORS, etc.]
│       └── NacWebApiModule.cs    [DependsOn NacCoreModule + NacObservabilityModule]
├── tests/
│   ├── Nac.Core.Tests/           [190 tests]
│   │   ├── Primitives/
│   │   ├── Results/
│   │   ├── Domain/
│   │   ├── Abstractions/
│   │   └── ValueObjects/
│   ├── Nac.Cqrs.Tests/           [23 tests]
│   ├── Nac.Caching.Tests/        [8 tests]
│   ├── Nac.Persistence.Tests/    [34 tests]
│   ├── Nac.EventBus.Tests/       [34 tests]
│   ├── Nac.Testing.Tests/        [47 tests]
│   ├── Nac.MultiTenancy.Tests/   [61 tests - Wave 2B]
│   ├── Nac.MultiTenancy.Management.Tests/ [38 tests - Wave 2B-Enhancement]
│   ├── Nac.Identity.Tests/       [100 tests - Wave 2B + Pattern A Phases 05–07]
│   ├── Nac.Observability.Tests/  [28 tests - Wave 2C]
│   ├── Nac.Jobs.Tests/           [21 tests - Wave 2C]
│   └── Nac.WebApi.Tests/         [42 tests - Wave 3]
├── docs/                         [Framework documentation]
├── plans/                        [Implementation plans & phase docs]
├── Directory.Build.props         [Shared MSBuild properties]
├── Directory.Packages.props      [Centralized version management]
├── nuget.config                  [NuGet feed configuration]
└── NacFramework.slnx            [Solution file]
```

## Key Design Decisions

| Area | Decision | Rationale |
|------|----------|-----------|
| **Dependency Graph** | L0 has zero external deps (only Microsoft.Extensions abstractions) | Maximize reusability, zero coupling to implementations |
| **Result Pattern** | Custom Result<T> inspired by Ardalis.Result | Explicit error handling, type-safe validation errors |
| **Specifications** | Boolean algebra (And/Or/Not) for domain queries | Composable, testable query logic |
| **Modularity** | DependsOn attributes + NacModule base | Declarative module dependencies, auto-wiring via DI |
| **DI Markers** | ITransientDependency, IScopedDependency, ISingletonDependency | Opt-in convention-based DI registration (L3+ feature) |

## Testing Strategy

- **Framework:** xUnit + FluentAssertions
- **Coverage:** Unit tests for all L0 primitives, results, value objects, and domain utilities
- **Test Organization:** Mirror source structure for discoverability
- **Approach:** Behavior-driven, parameterized tests where applicable

### Test Files (11 total)
- `ValueObjectTests` — Equality, hashing, immutability
- `EntityTests` — ID comparison, events, soft-delete
- `AggregateRootTests` — Event sourcing, state management
- `ResultTests` — Success, failures, validation errors
- `ResultTTests` — Generic result with typed payloads
- `SpecificationTests` — Boolean composition, filtering
- `GuardTests` — Null checks, string validation, numeric bounds
- `PermissionDefinitionTests` — Permission hierarchy, group logic
- `PaginationTests` — Offset, limit, computed properties
- `MoneyTests` — Value equality, currency handling
- `DateRangeTests` — Overlap, duration, boundary conditions

## Current Implementation Completeness

### Implemented (L0 + Wave 1 + Wave 2A + Wave 2B)
- ✅ Solution infrastructure (.NET 10, MSBuild, NuGet, CI-ready)
- ✅ Results pattern (Result, Result<T>, ValidationError, ResultStatus)
- ✅ Primitives (Entity, AggregateRoot, ValueObject, StronglyTypedId, DomainEvent)
- ✅ Domain utilities (Repository interfaces, Specification, Guard, DomainError)
- ✅ DI markers and attributes
- ✅ Modularity system (NacModule, DependsOn)
- ✅ Abstractions (Identity, Permissions, Events, DateTime, MultiTenancy)
- ✅ Data seeding interfaces
- ✅ Value objects (Money, Address, DateRange, Pagination)
- ✅ CQRS layer (L1) — Custom dispatcher, sealed handlers, 4 pipeline behaviors
- ✅ Caching layer (L1) — INacCache abstraction, tenant-aware keys, tag invalidation
- ✅ Persistence layer (L2) — NacDbContext, Repository impl, 4 EF Core interceptors, Outbox pattern
- ✅ EventBus layer (L2) — IEventPublisher/IEventHandler<T>/IEventDispatcher, InMemory transport (Channels), Outbox bridge, assembly scanning
- ✅ Testing layer (L2) — 7 in-memory fakes, fluent builders, DI fixtures, assertion extensions
- ✅ MultiTenancy layer (L2) — 4 resolution strategies, RLS query filters, TenantEntityInterceptor, per-tenant DB support
- ✅ Identity layer (L2) — NacUser/NacRole, IdentityService, JwtTokenService, permission system with hierarchical checks
- ✅ Unit tests (626 tests, all passing)
- ✅ Pattern A Identity Migration (Phases 01–07: Domain, Services, Auth, Roles, Membership, Admin, Host Permissions)

### Not Yet Implemented
- WebApi composition root (L3)
- Templates (dotnet new)
- Examples (SimpleCrud, SaaSStarter, MicroserviceExtract)

## Dependency Analysis

**Nac.Core External Dependencies:**
- `Microsoft.Extensions.DependencyInjection.Abstractions` — DI contracts only
- `Microsoft.Extensions.Configuration.Abstractions` — Configuration contracts only

No third-party business logic dependencies (result pattern is custom, not external).

## Code Quality Standards

| Aspect | Standard |
|--------|----------|
| **Naming** | PascalCase (types, methods, properties), camelCase (parameters, locals) |
| **File Size** | Target <150 lines; complex logic refactored to separate files |
| **Null Handling** | Nullable reference types enabled; explicit null checks with Guard |
| **Immutability** | Value objects and domain events immutable; entities track state changes |
| **Error Handling** | Result pattern for business errors; exceptions for violations |
| **Comments** | Minimal; code is self-documenting via clear naming |

## File Manifest (Nac.Core Source)

| File | Purpose | LOC |
|------|---------|-----|
| Primitives/Entity.cs | Base class for domain entities | ~40 |
| Primitives/AggregateRoot.cs | Aggregate root with event sourcing | ~35 |
| Primitives/ValueObject.cs | Base class for value objects | ~25 |
| Results/Result.cs | Non-generic result type | ~38 |
| Results/ResultT.cs | Generic result with payload | ~55 |
| Domain/Specification.cs | Composable query specification | ~80 |
| Domain/Guard.cs | Guard clauses utility | ~65 |
| Modularity/NacModule.cs | Base module class | ~45 |
| Abstractions/Permissions/PermissionDefinition.cs | Permission model | ~40 |
| ValueObjects/Money.cs | Money value object | ~35 |
| ValueObjects/DateRange.cs | Date range value object | ~45 |
| ValueObjects/Pagination.cs | Pagination parameters | ~30 |

## Version & Release

- **Framework Version:** Managed via `Directory.Packages.props` (NacFrameworkVersion)
- **Target Framework:** .NET 10.0
- **Release Strategy:** All packages version-locked to framework version
- **NuGet Feed:** Private feed configured in `nuget.config`

---

## NuGet Package Dependencies (Wave 3)

**New WebApi Dependencies:**
- Asp.Versioning.Http 8.1.0
- Asp.Versioning.Mvc.ApiExplorer 8.1.0
- Microsoft.AspNetCore.OpenApi 10.0.6

**Identity Dependencies (Wave 2B):**
- Microsoft.AspNetCore.Authentication.JwtBearer 10.0.6
- Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.6
- Microsoft.IdentityModel.Tokens 8.0.1
- System.IdentityModel.Tokens.Jwt 8.0.1

**Framework Dependencies (Core):**
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.6
- Microsoft.Extensions.Configuration.Abstractions 10.0.6
- FluentValidation 12.1.1
- Microsoft.EntityFrameworkCore 10.0.6
- Microsoft.EntityFrameworkCore.Relational 10.0.6
- System.Threading.Channels 8.0.0

**Test Dependencies:**
- xunit.v3 3.2.2
- FluentAssertions 8.9.0
- NSubstitute 5.3.0

---

## Consumer Reference Architecture

**Location:** `samples/ReferenceApp/` — Canonical consumer project blueprint demonstrating framework usage.

**Includes:**
- Orders module (CQRS, DDD, OrderCreatedEvent publication)
- Billing module (Event handler subscribing to cross-module events)
- Shared kernel (permission definitions, result extensions, DbContext base)
- External Postgres 17 (user-managed; sample no longer ships a docker-compose file — user points `ConnectionStrings:Default` at an existing instance)
- Integration tests: 11 passing (CRUD, permissions, multi-tenancy, cross-module events, JWT)
- Per-module DbContext with schema isolation + migration workflows

**Notable fixes shipped with consumer blueprint:**
- **AddNacEventBus idempotency:** EventHandlerRegistry now safely handles multiple calls; shared channel prevents event loss
- **Known limitation documented:** OutboxWorker resolves single NacDbContext (last-registered-wins); workaround documented in sample

---

**Last Updated:** 2026-04-17 (Wave 3 + Consumer Reference Architecture) | **Total Tests:** 577 framework + 11 consumer integration
