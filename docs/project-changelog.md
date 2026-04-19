# NAC Framework — Project Changelog

All significant changes to the NAC Framework are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/).

---

## [Unreleased] — Pattern A Identity Migration Phases 01–04 (2026-04-19)

### Added (In Development)

#### Phase 01: Domain Model Refactor
- **NacUser** refactored: removed `TenantId`, added `IsHost` flag; global scope
- **UserTenantMembership** M:N join table: `UserTenantMembership(Id, UserId, TenantId, Status, JoinedAt, InvitedBy?, IsDefault)`
- **MembershipRole** join table: `MembershipRole(MembershipId, RoleId)` for tenant-scoped role assignment
- **NacRole** expanded: nullable `TenantId`, `IsTemplate` flag, `Description`, `BaseTemplateId` for lineage
- **PermissionGrant** table: ABP-style `(ProviderName="U"|"R", ProviderKey, PermissionName, TenantId?)` replacing `RolePermission` + claim-based storage + `PermissionSet` concept
- **PermissionProviderNames** constants: `User="U"`, `Role="R"`
- EF configurations for all new entities with composite unique indices (PG NULL-distinct)
- Schema migration support: `Initial_PatternA` migration

#### Phase 02: Identity Services Refactor
- **JwtTokenService** pure method signature: no DB reads; minimal claims (`sub`, `email`, `name`, `tenant_id?`, `role_ids[]`, `is_host?`); no permission claims
- **IPermissionGrantRepository** + **EfCorePermissionGrantRepository**: direct `PermissionGrant` queries (U/R providers)
- **IPermissionGrantCache** + **DistributedPermissionGrantCache**: wraps `IDistributedCache`; supports pattern-based invalidation
- **PermissionChecker** rewrite: ABP-style resolution (user + role grants union) + tree-walk ancestry + 10min TTL + instant invalidation
- **IMembershipService** + **MembershipService**: Invite (token generation), Accept, List, ChangeRoles (invalidates cache), CreateActiveMembershipAsync (onboarding)
- **ITenantSwitchService** + **TenantSwitchService**: validates Active membership; re-issues tenant-scoped JWT
- **IRoleService** shell (Phase 04 completes): CRUD, grant/revoke, clone, list
- **ICurrentUser** shape change: `TenantId` (current selection from JWT), `RoleIds` (Guid[]), `IsHost`
- **NacIdentityClaims** constants: `TenantId="tenant_id"`, `RoleIds="role_ids"`, `IsHost="is_host"`
- DI: default `MemoryDistributedCache`; prod swaps Redis via DI

#### Phase 03: Auth Endpoints
- **AllowTenantlessAttribute** marker for tenantless endpoints (login, switch, memberships)
- **ClaimTenantStrategy** reads `tenant_id` claim; optional fallback (Header/Route/Default)
- **TenantRequiredGateMiddleware** gates tenant-scoped endpoints; 403 if tenant null
- **AuthEndpoints** (Minimal API, 7 routes):
  - `POST /auth/login` → tenantless token + memberships list (client picks tenant)
  - `POST /auth/switch-tenant` → tenant-scoped token + roleIds
  - `POST /auth/refresh` → preserves tenant scope (v3 stub: 501)
  - `POST /auth/logout` → (refresh-token revocation placeholder)
  - `GET /auth/me` → current user snapshot + claims
  - `GET /auth/memberships` → tenantless list of user's tenant memberships
  - `POST /auth/accept-invitation` → flip Invited→Active
- **Contracts**: LoginRequest/Response, SwitchTenantRequest/Response, MembershipListItem, MeResponse, AcceptInvitationRequest
- Problem+json error shapes for 401/403/501
- Wire-up: services registered; middleware registered; host calls `MapNacAuthEndpoints()`

#### Phase 04: Role Template System
- **IRoleTemplateProvider** DSL for registration; idempotent seeder at startup
- **RoleTemplateDefinition** record: `(Key, Name, Description, PermissionNames[])`
- **RoleTemplateDefinitionManager** FrozenDictionary registry; lookup by key
- **RoleTemplateBuilder** fluent API: `.AddTemplate(...).Grants(...)` 
- **RoleTemplateKeyHasher** deterministic Guid from key (MD5 hash) — idempotent seeding
- **DefaultRoleTemplateProvider** ships: `owner`, `admin`, `member`, `guest` templates with default permission sets
- **RoleTemplateSeeder** (IHostedService) idempotent upsert:
  - Insert `NacRole(Id=hashedGuid, IsTemplate=true, TenantId=null)` per template
  - Upsert `PermissionGrant(ProviderName="R", TenantId=null)` for template permissions
  - Diff + remove stale grants (template evolution)
  - DB-ready wait (retry with backoff)
- **IRoleService** full implementation:
  - `CloneFromTemplateAsync(tenantId, templateRoleId, name)` → creates tenant role + copies grants + sets `BaseTemplateId`
  - `CreateAsync(tenantId, name)` → custom role with no grants
  - `GrantPermissionAsync/RevokePermissionAsync` → mutate grants + invalidate cache
  - `ListGrantsAsync, ListTemplatesAsync, DeleteAsync` (soft-delete if unreferenced)
- Wire-up: `AddNacRoleTemplates()` extension; registered in `ServiceCollectionExtensions`

### Status
- **Phases 01–04:** COMPLETE (implementation + code review APPROVED_WITH_FIXES)
- **Phases 05–09:** PENDING
- **Test coverage:** Phase 08 deferred (pre-existing compile errors from Phase 01/02 builder changes; risk R10)
- **Code review:** 8/10 score; 1 critical fixed (NormalizedName on role clone), 1 major fixed (error-code casing NAC_INVALID_CREDENTIALS)

### Known Issues / Deferred
- **M2/M3/M4 + 11 minor review items** → Phase 07 cleanup OR Phase 05 follow-ups (see plans/reports/code-reviewer-260419-1520-phase-03-04.md)
- **R10 (test builders)** → Phase 08 responsibility; 52 pre-existing compile errors in `tests/Nac.Identity.Tests`

---

## [1.6.0] — Tenant Management Module (2026-04-19)

### Added

#### Nac.MultiTenancy.Management (L2)
- **Tenant Aggregate:** AggregateRoot<Guid> with IAuditableEntity + ISoftDeletable
- **Domain Events:** 5 events (TenantCreatedEvent, TenantUpdatedEvent, TenantDeletedEvent, TenantActivatedEvent, TenantDeactivatedEvent) implementing both IDomainEvent and IIntegrationEvent
- **Registry Database:** TenantManagementDbContext (central registry, not multi-tenant)
- **Tenant Store Implementation:** EfCoreTenantStore with 10-minute sliding cache override for ITenantStore
- **Encrypted Connection Strings:** EncryptedConnectionStringResolver using Microsoft.AspNetCore.DataProtection (purpose: `Nac.MultiTenancy.Management.ConnectionString`)
- **REST API:** 11 endpoints at `/api/admin/tenants`:
  - POST / GET / GET{id} / GET by-identifier / PUT / DELETE
  - POST {id}/activate, {id}/deactivate
  - POST bulk/activate, bulk/deactivate, bulk/delete
- **Authorization:** All endpoints require `[Authorize(Policy = "Tenants.Manage")]` + `HostAdminOnlyFilter` (rejects non-host callers)
- **Bulk Operations:** Best-effort with 207 Multi-Status on partial failure (max 100 IDs per request)
- **DI Entry Point:** `services.AddNacTenantManagement(opts => opts.UseDbContext(...))`
- **Cache Invalidation:** Manual via ITenantCacheInvalidator; automatic on all mutations
- **Outbox Integration:** All mutations emit domain events → Outbox → IntegrationEventPublisher

### Tests
- 38 unit tests covering domain aggregate, EF-backed store, encryption round-trip, all 11 REST endpoints, bulk operations, authorization, outbox emission
- All tests passing

### Documentation
- Created src/Nac.MultiTenancy.Management/README.md with installation, quickstart, API reference, key features, and DataProtection key persistence warnings
- Updated docs/project-overview-pdr.md: Marked PDR 5 (Nac.MultiTenancy) as complete; added PDR 5A for Nac.MultiTenancy.Management
- Updated docs/codebase-summary.md: Added L2 row + codebase structure entry
- Updated docs/system-architecture.md: Added Tenant Management Registry section with admin/runtime flows
- Updated docs/project-changelog.md (this file)
- Updated docs/project-roadmap.md: Marked tenant-management phase as completed

### Dependencies (New)
- Microsoft.AspNetCore.DataProtection (framework API)

### Breaking Changes
- None. Full backward compatibility maintained. Nac.MultiTenancy remains functional without this module.

---

## [1.5.1] — Consumer Reference Architecture & Framework Fixes (2026-04-17)

### Added

#### samples/ReferenceApp (Consumer Blueprint)
- Orders module: Order aggregate, CreateOrder command, GetOrderById query, OrderCreatedEvent integration event
- Billing module: Customer entity, OrderCreatedEvent handler → automatic Invoice creation
- Multi-module event flow: Orders outbox → EventBus → Billing event handler with automatic tenant propagation
- Integration tests: 11 passing tests (CRUD, permissions, cross-module events, idempotency, JWT auth)
- Multi-schema design (Orders + Billing per-module DbContext) against an external user-managed Postgres (no docker-compose bundled)
- Permission definitions + policy-based authorization ([Authorize(Policy="Orders.Create")])
- Copy-rename blueprint (replaces `dotnet new` in v1)

### Fixed (Nac.EventBus)
- **AddNacEventBus idempotency:** Safe to call multiple times from module + host compositions
- **EventHandlerRegistry refactored:** Split into RegisterHandlers (per-call DI scan) + BuildRegistry (lazy FrozenDictionary)
- **Root cause:** Triple-channel/single-worker mismatch causing events dispatched into channel with no reader
- **Impact:** Multi-module solutions can now safely register event handlers; idempotent registration prevents lost events

### Known Limitation (Framework-Level, Non-Breaking)
- **OutboxWorker single-context constraint:** Resolves only last-registered NacDbContext alias; workaround is to order [DependsOn] so publishing module registers last
- **Planned fix:** Generic `OutboxWorker<TContext>` in roadmap (v1.6+)
- **Consumer impact:** Billing sample keeps Outbox disabled (no events published from Billing v1); Orders module works correctly

### Documentation Updates
- Created samples/ReferenceApp/README.md — setup, architecture, deployment
- Rewrote NAC-Consumer-Project-Architecture.md — 938→972 lines, 17 sections, no aspirational features
- Added consumer limitation note (OutboxWorker single-context) + workaround

---

## [1.5.0] — Wave 3: WebApi Composition Root (2026-04-17)

### Added

#### Nac.WebApi (L3 Composition Root)
- NacWebApiModule: Composition root module [DependsOn(NacCoreModule, NacObservabilityModule)]
- NacWebApiOptions: Configuration toggles (EnableApiVersioning, EnableOpenApi, EnableCors, EnableRateLimiting, EnableResponseCompression, EnableHealthChecks)
- AddNacWebApi() extension: Options configuration entry point
- UseNacApplication() extension: 13-stage middleware pipeline with conditional inclusion
- NacExceptionHandler: RFC 9457 ProblemDetails global exception handler
- ResultToHttpMapper: 6 ResultStatus → HTTP status code mapping (Ok/Invalid/NotFound/Forbidden/Conflict/Error)
- NacModuleLoader (Nac.Core): Kahn's algorithm topological sort with cycle detection
- NacApplicationFactory (Nac.Core): Pre/Config/Post lifecycle orchestration
- NacApplicationLifetime (Nac.Core): IHostedService for Init/Shutdown hooks
- API versioning: Asp.Versioning 8.1.0 + Asp.Versioning.Mvc.ApiExplorer 8.1.0
- OpenAPI integration: Microsoft.AspNetCore.OpenApi 10.0.6 (/openapi endpoint)
- Middleware order: ExceptionHandler → HTTPS → Compression → Routing → RateLimiter → CORS → MultiTenancy → Auth → Authz → Observability → Controllers → OpenAPI → HealthChecks
- Conditional middleware: MultiTenancy/Observability only if module in DependsOn graph

### Module System (Enhanced in Nac.Core)
- NacModule: Three-phase lifecycle (PreConfigureServices, ConfigureServices, PostConfigureServices)
- DependsOnAttribute: Declarative module dependencies (attribute-based graph)
- NacModuleLoader: Type discovery + topological sort + cycle detection
- NacApplicationFactory: Module lifecycle orchestration
- NacApplicationLifetime: Initialization/shutdown triggers

### Tests
- 42 new unit tests for Nac.WebApi composition root
- Covers: module discovery, middleware pipeline, exception handling, options configuration
- Integration tests for middleware ordering and conditional inclusion
- Total test count: 577 (all passing) [190 + 23 + 8 + 34 + 34 + 47 + 89 + 61 + 28 + 21 + 42]

### Documentation
- Updated system-architecture.md with L3 WebApi section (Section 12: Module System & WebApi Composition)
- Updated codebase-summary.md with Nac.WebApi package details
- Updated project-changelog.md (this file)
- Updated project-roadmap.md with Wave 3 completion

### Breaking Changes
- None. Full backward compatibility maintained.

### Dependencies
**New:**
- Asp.Versioning.Http 8.1.0
- Asp.Versioning.Mvc.ApiExplorer 8.1.0
- Microsoft.AspNetCore.OpenApi 10.0.6

---

## [1.4.0] — Wave 2C: Observability & Jobs (2026-04-16)

### Added

#### Nac.Observability (L2)
- LoggingEnricherMiddleware: HTTP request context enrichment (tracing, user, tenant, timing)
- NacActivitySources: OpenTelemetry activity source name constants
- NacMeters: Meter name constants for metrics collection
- NacLoggingScope: Fluent API for scoped logging context (WithUserId, WithTenantId, WithCustomField)
- AddNacObservability() extension: Middleware registration and diagnostics setup
- NacObservabilityModule: Modular registration

#### Nac.Jobs (L2)
- IJobScheduler interface: Enqueue and schedule one-time/delayed jobs
- IRecurringJobManager interface: Register and manage recurring job schedules (cron expressions)
- IJobHandler<TJob> interface: Job execution contract
- JobDefinition class: Immutable job metadata (Id, Type, Parameters, ScheduledAt, RetryCount)
- FakeJobScheduler: Testing implementation (collects scheduled jobs)
- FakeRecurringJobManager: Testing implementation (tracks operations)
- AddNacJobs() extension: Services registration with testing fakes
- NacJobsModule: Modular registration

### Tests
- 49 new unit tests for Observability and Jobs packages
- Nac.Observability.Tests: 28 tests covering logging enrichment, diagnostics, scope handling
- Nac.Jobs.Tests: 21 tests covering scheduling abstractions, recurring jobs, fakes
- All tests passing
- Total test count: 535 (all passing)

### Documentation
- Updated system-architecture.md with Observability and Jobs layers (Sections 10 & 11)
- Updated codebase-summary.md with Wave 2C package details
- Updated project-roadmap.md with Wave 2C completion and revised timeline
- Updated project-changelog.md (this file)

---

## [1.3.0] — Wave 2B: Identity & MultiTenancy (2026-04-16)

### Added

#### Nac.Identity (L2)
- NacUser: ASP.NET Core Identity user entity extending IdentityUser<Guid>
- NacRole: ASP.NET Core Identity role entity extending IdentityRole<Guid>
- NacIdentityDbContext: DbContext base class with manual Identity table configuration
- NacIdentityDbContext<TUser>: Generic variant for consumer-extended user types
- CurrentUserAccessor: ICurrentUser implementation from ClaimsPrincipal claims mapping
- IdentityService: IIdentityService wrapper over UserManager<TUser>
- JwtTokenService: JWT token generation with claims (userId, email, tenantId, roles, permissions)
- JwtOptions: JWT configuration (secret, issuer, audience, expiration)
- NacIdentityClaims: Constants for standard claim names
- PermissionDefinitionContext: DSL for permission definition (groups, permissions, hierarchy)
- PermissionDefinitionManager: FrozenDictionary registry with lookup and iteration
- PermissionChecker: IPermissionChecker with claim validation, hierarchical grants, default permissions
- PermissionAuthorizationHandler: IAuthorizationHandler for [Authorize(Policy = "Permission.X")] attributes
- PermissionRequirement: AuthorizationHandler requirement
- NacIdentityOptions: Configuration model
- AddNacIdentity() extension: JWT Bearer auth, Identity, permission services, discovery
- NacIdentityModule: Modular registration

#### Nac.MultiTenancy (L2)
- TenantInfo: Tenant model with Id, Name, ConnectionString, IsActive, Properties
- ITenantContext: Ambient context for current tenant
- ITenantStore: Tenant lookup contract (GetByIdAsync, GetByNameAsync, ListAsync)
- TenantContext: AsyncLocal<TenantInfo>-based ambient context implementation
- InMemoryTenantStore: In-memory IRepository-based implementation
- MultiTenancyOptions: Configuration (EnablePerTenantDatabase flag)
- ITenantResolutionStrategy: Strategy interface for tenant resolution
- HeaderTenantStrategy: X-Tenant-Id header extraction
- ClaimTenantStrategy: tenant_id claim extraction
- RouteTenantStrategy: {tenantId} route parameter extraction
- SubdomainTenantStrategy: Subdomain-based extraction (tenant.example.com pattern)
- TenantResolutionMiddleware: Middleware pipeline trying strategies in order
- NacTenantHeaders: Constant for X-Tenant-Id header
- NacTenantClaims: Constant for tenant_id claim
- MultiTenantDbContext: DbContext with tenant query filter (IQueryable<T>.Where(e => e.TenantId == tenantId))
- TenantEntityInterceptor: SaveChangesInterceptor auto-setting TenantId for new ITenantEntity objects
- ITenantConnectionStringResolver: Connection string resolution contract
- TenantConnectionStringResolver: Default resolver (ITenantStore → ConnectionString)
- AddNacMultiTenancy() extension: Services registration
- UseNacMultiTenancy() extension: Middleware registration
- NacMultiTenancyModule: Modular registration

### Tests
- 150+ new unit tests for Identity and MultiTenancy packages
- Nac.Identity.Tests: 75+ tests covering JWT, permissions, claims, user context
- Nac.MultiTenancy.Tests: 75+ tests covering tenant resolution, query filters, context switching
- All tests passing
- Total test count: 486 (all passing)

### Documentation
- Updated system-architecture.md with Identity and MultiTenancy layers
- Updated codebase-summary.md with Wave 2B package details
- Updated project-roadmap.md with Wave 2B completion and revised timeline
- Updated project-changelog.md (this file)

---

## [1.2.0] — Wave 2A: EventBus & Testing (2026-04-16)

### Added

#### Nac.EventBus (L2)
- IEventPublisher interface for publishing integration events
- IEventHandler<TEvent> interface for event consumption
- IEventDispatcher interface for routing events to handlers
- InMemoryEventBus implementation using System.Threading.Channels (bounded, 1000 capacity)
- InMemoryEventBusWorker BackgroundService for asynchronous event processing
- EventDispatcher with FrozenDictionary-based handler registry for O(1) lookup
- EventHandlerRegistry with assembly scanning for automatic handler discovery
- Fan-out dispatch pattern: one failing handler doesn't block others (logged and swallowed)
- OutboxEventPublisher bridge: connects Persistence outbox to event bus (IIntegrationEventPublisher impl)
- OutboxEventTypeRegistry for safe event type deserialization (allowlist pattern)
- NacEventBusModule for modular registration
- AddNacEventBus() extension method with handler assembly registration

#### Nac.Testing (L2)
- NacTestFixture: Pre-configured DI container with all fakes
- InMemoryDbContextFixture<TContext>: EF Core in-memory database factory (isolated per test)
- 7 In-Memory Fake Implementations:
  - FakeCurrentUser: ICurrentUser with settable Id, Name, IsAuthenticated, Roles
  - FakeDateTimeProvider: IDateTimeProvider with settable UtcNow
  - FakePermissionChecker: IPermissionChecker with grant/deny lists (GrantAll, DenyAll factories)
  - FakeRepository<T>: IRepository<T> with in-memory storage, operation tracking (Added/Updated/Deleted)
  - FakeEventPublisher: IEventPublisher collecting published events (no dispatch)
  - FakeSender: ISender (CQRS) collecting sent commands/queries
  - FakeNacCache: INacCache with in-memory storage and tag invalidation tracking
- TestEntityBuilder<TEntity, TBuilder>: Abstract fluent builder with reflection-based property setting
- ResultBuilder: Fluent builder for Result<T> construction
- ResultAssertionExtensions: FluentAssertions helpers (BeSuccess, BeFailed, HaveStatus)
- AddNacTesting() extension method for registering all fakes

### Tests
- 80+ new unit tests for EventBus and Testing packages
- All tests passing
- Total test count: 255+ (all passing)

### Documentation
- Updated system-architecture.md with EventBus and Testing layers (Sections 6 & 7)
- Updated codebase-summary.md with Wave 2A package details
- Updated project-roadmap.md with Wave 2A completion and revised timeline
- Updated project-changelog.md (this file)

---

## [1.1.0] — Wave 1 Completion + Dependency Upgrades (2026-04-16)

### Changed (Dependency Upgrades)
- **EntityFrameworkCore:** 10.0.0 → 10.0.6 (patch update for bug fixes and stability)
- **FluentValidation:** 11.11.0 → 12.1.1 (major version with enhanced validation capabilities)
- **Microsoft.Extensions.Caching.Hybrid:** 9.6.0 → 10.4.0 (upgraded to .NET 10+ version)
- **xUnit:** 2.9.3 → xUnit.v3 3.2.2 (major version with improved runner and analysis)
  - **Breaking Change:** Test projects now require `OutputType=Exe` in project files
  - Added suppression for xUnit1051 analyzer in test projects

### Notes
- All Wave 1 packages (CQRS, Caching, Persistence) updated and tested with new dependency versions
- No functional changes; all 255 unit tests passing with upgraded dependencies
- HybridCache documentation updated to reflect .NET 10+ support

---

## [1.1.0] — Wave 1 Completion (2026-04-16)

### Added

#### Nac.Cqrs (L1)
- Custom CQRS dispatcher using FrozenDictionary for O(1) handler lookup
- ICommand and IQuery interfaces with sealed generic handler pattern
- ValueTask<T> async return types for optimal performance
- ISender dispatch interface for request processing
- 4 Pipeline Behaviors:
  - ValidationBehavior (FluentValidation integration)
  - LoggingBehavior (ILogger integration)
  - CachingBehavior (INacCache integration)
  - TransactionBehavior (IUnitOfWork integration)
- Marker interfaces: ICacheableQuery, ITransactionalCommand
- Assembly scanning for automatic handler registration
- AddNacCqrs() extension method for DI setup
- Full integration with Nac.Core modularity system

#### Nac.Caching (L1)
- INacCache abstraction over HybridCache (.NET 10+)
- Tenant-aware key prefixing via ICurrentUser context
- Tag-based cache invalidation patterns
- CacheKey static utility for consistent key construction
- CacheEntryOptions configuration model
- AddNacCaching() extension method for DI setup
- Full Nac.Core integration

#### Nac.Persistence (L2)
- NacDbContext abstract base class (DB-agnostic EF Core 10)
- Repository<T> generic implementation (IRepository + IReadRepository)
- Specification to IQueryable bridge (SpecificationExtensions)
- 4 EF Core Interceptors:
  - AuditableEntityInterceptor (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy tracking)
  - SoftDeleteInterceptor (ISoftDeletable support)
  - DomainEventInterceptor (IDomainEvent sourcing)
  - OutboxInterceptor (Transactional Outbox pattern)
- OutboxEvent model and configuration
- OutboxWorker BackgroundService for reliable event processing
- AddNacPersistence<TContext>() extension method for DI setup
- Full Nac.Core integration

#### Nac.Core Additions
- IUnitOfWork interface for transactional boundary marking
- IHasDomainEvents interface for entities with domain events

### Tests
- 65 new unit tests for Wave 1 packages
- Total test count: 255 (all passing)
- Test coverage: 100% for new implementations

### Documentation
- Updated system-architecture.md with L1 and L2 package details
- Updated codebase-summary.md with Wave 1 structure
- Updated project-overview-pdr.md with Wave 1 PDRs marked complete
- Created project-changelog.md (this file)

---

## [1.0.0] — Initial L0 Release (2026-04-08)

### Added

#### Nac.Core (L0 - Zero External Dependencies)

**Primitives:**
- Entity<TId> — Base class for domain entities with event sourcing
- AggregateRoot<TId> — Transactional boundary marker
- ValueObject — Record-based immutable value types
- IDomainEvent — Domain event marker interface
- IStronglyTypedId — Strongly-typed ID support
- IAuditableEntity — CreatedAt/ModifiedAt tracking interface
- ISoftDeletable — Logical deletion marker interface

**Results Pattern:**
- Result — Non-generic success/failure type
- Result<T> — Generic result with typed payloads
- ResultStatus enum — Ok, Invalid, NotFound, Forbidden, Conflict, Error
- ValidationError — Field-level validation error model

**Domain:**
- IRepository<T> — Write operations contract
- IReadRepository<T> — Query operations contract
- Specification<T> — Composable query logic with And/Or/Not operators
- Guard — Input validation utility with NotNull, NotEmpty, GreaterThanOrEqual, Length methods
- DomainError — Domain error marker
- ITenantEntity — Multi-tenancy marker interface

**Modularity:**
- NacModule — Base module class with lifecycle hooks
- DependsOnAttribute — Module dependency declaration
- ServiceConfigurationContext — DI context for ConfigureServices
- ApplicationInitializationContext — DI context for initialization
- ApplicationShutdownContext — DI context for shutdown

**Dependency Injection:**
- ITransientDependency — Convention marker for transient scope
- IScopedDependency — Convention marker for scoped scope
- ISingletonDependency — Convention marker for singleton scope
- DependencyAttribute — Explicit DI registration attribute

**Abstractions:**
- ICurrentUser — Current user context
- IIdentityService — Authentication contract
- UserInfo — User information DTO
- PermissionDefinition — Permission model with hierarchy
- PermissionGroup — Permission group organization
- IPermissionChecker — Permission validation contract
- IPermissionDefinitionProvider — Permission definition registry
- IIntegrationEvent — Integration event marker
- UserRegisteredEvent, UserEmailConfirmedEvent, PasswordResetEvent — Concrete events
- IDateTimeProvider — DateTime abstraction for testability

**Data Seeding:**
- IDataSeeder — Data seeding contract
- DataSeedContext — Seeding context information

**Value Objects:**
- Money — Immutable money type with currency
- Address — Immutable address with street, city, state, postal code
- DateRange — Immutable date range with overlap/duration checks
- Pagination — Offset/limit pagination parameters

### Tests
- 190 unit tests (xUnit + FluentAssertions)
- 100% test coverage
- All tests passing

### Project Infrastructure
- .NET 10.0 LTS target framework
- Directory.Build.props for shared MSBuild configuration
- Directory.Packages.props for centralized NuGet version management
- nuget.config for NuGet feed configuration
- Solution file (NacFramework.slnx)
- MIT License

### Documentation
- system-architecture.md — Detailed architecture overview
- codebase-summary.md — Codebase structure and organization
- project-overview-pdr.md — PDR for L0 package
- code-standards.md — Coding conventions and patterns

---

## Version Numbering

NAC Framework follows semantic versioning:
- **Major.Minor.Patch** (e.g., 1.0.0, 1.1.0, 2.0.0)
- All packages version-locked to framework version
- Breaking changes trigger major version bump
- New features trigger minor version bump
- Bug fixes and patches trigger patch version bump

---

## Release Timeline

| Release | Date | Focus | Status |
|---------|------|-------|--------|
| 1.0.0 | 2026-04-08 | L0 Nac.Core (190 tests) | ✅ Complete |
| 1.1.0 | 2026-04-16 | Wave 1: L1 CQRS/Caching + L2 Persistence (255 tests) | ✅ Complete |
| 1.2.0 | 2026-04-16 | Wave 2A: L2 EventBus + Testing (80+ tests, 255+ total) | ✅ Complete |
| 1.3.0 | 2026-04-16 | Wave 2B: L2 Identity + MultiTenancy (150+ tests, 486 total) | ✅ Complete |
| 1.4.0 | 2026-04-16 | Wave 2C: L2 Observability, Jobs (49 tests, 535 total) | ✅ Complete |
| 1.5.0 | 2026-04-17 | Wave 3: L3 WebApi Composition Root (42 tests, 577 total) | ✅ Complete |
| 2.0.0 | 2026-Q4 | Templates, Examples, Production-ready | 📋 Planned |

---

**Last Updated:** 2026-04-17 (Wave 3 completion)  
**Maintainer:** Solo development  
**License:** MIT
