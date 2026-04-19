# NAC Framework — Project Overview & Product Development Requirements

## Project Overview

**Project Name:** NAC Framework  
**Type:** Foundational Framework (NuGet Package Suite)  
**Target Framework:** .NET 10 LTS  
**License:** MIT  
**Repository Type:** Mono-repo (single git repo, multiple NuGet packages)  
**Current Phase:** Wave 1 Complete (L0 + L1 Cqrs/Caching + L2 Persistence)  
**Team:** Solo development

### Vision
Build a modular, DDD-based .NET framework published as reusable NuGet packages. Enable rapid SaaS/enterprise application development with built-in patterns for multi-tenancy, CQRS, event sourcing, identity, and observability.

### Mission
- Provide zero-dependency DDD building blocks (L0)
- Layer abstractions for cross-cutting concerns (L1-L3)
- Publish production-ready NuGet packages with comprehensive tests
- Include reference examples and templates for rapid consumer adoption

### Key Metrics
| Metric | Target | Current |
|--------|--------|---------|
| L0 Nac.Core Completion | 100% | 100% ✅ |
| L1 Nac.Cqrs Completion | 100% | 100% ✅ (Wave 1) |
| L1 Nac.Caching Completion | 100% | 100% ✅ (Wave 1) |
| L2 Nac.Persistence Completion | 100% | 100% ✅ (Wave 1) |
| Unit Test Coverage | 80%+ | 100% (255 tests, all passing) |
| External Dependencies (L0) | 0 (custom code) | 2 (MS abstractions only) |
| Package Count | 12 layers | 4 (L0 + 3 Wave 1) |
| Documentation | All packages documented | Core + Wave 1 documented |

---

## Product Development Requirements (PDR)

### PDR 1: L0 Core Package (COMPLETE)

**Status:** ✅ Complete & Tested  
**Acceptance Criteria:** All met

#### Functional Requirements
| ID | Requirement | Status |
|----|-------------|--------|
| FR-L0-01 | Result pattern with Result and Result<T> types | ✅ |
| FR-L0-02 | Domain primitives: Entity, AggregateRoot, ValueObject, DomainEvent | ✅ |
| FR-L0-03 | Repository interfaces (IRepository, IReadRepository) | ✅ |
| FR-L0-04 | Specification pattern with boolean composition (And/Or/Not) | ✅ |
| FR-L0-05 | Guard clauses for input validation | ✅ |
| FR-L0-06 | DI marker interfaces for convention-based registration | ✅ |
| FR-L0-07 | Module system (NacModule, DependsOn attribute) | ✅ |
| FR-L0-08 | Identity abstractions (ICurrentUser, IIdentityService, UserInfo) | ✅ |
| FR-L0-09 | Permission definitions and hierarchy | ✅ |
| FR-L0-10 | Integration event abstractions + concrete events (UserRegistered, etc.) | ✅ |
| FR-L0-11 | Data seeding interfaces | ✅ |
| FR-L0-12 | Value objects (Money, Address, DateRange, Pagination) | ✅ |
| FR-L0-13 | IDateTimeProvider abstraction | ✅ |
| FR-L0-14 | StronglyTypedId support | ✅ |
| FR-L0-15 | Soft-delete and audit entity interfaces | ✅ |
| FR-L0-16 | Multi-tenancy entity interface | ✅ |

#### Non-Functional Requirements
| ID | Requirement | Status |
|----|-------------|--------|
| NFR-L0-01 | Zero external dependencies (only MS abstractions) | ✅ |
| NFR-L0-02 | Target .NET 10.0 | ✅ |
| NFR-L0-03 | Nullable reference types enabled | ✅ |
| NFR-L0-04 | All public APIs documented with XML comments | ✅ |
| NFR-L0-05 | 80%+ test coverage | ✅ |
| NFR-L0-06 | All tests passing (xUnit + FluentAssertions) | ✅ (190/190) |

---

### PDR 2: L1 CQRS Package (COMPLETE ✅)

**Status:** ✅ Complete | **Wave:** 1  
**Tests:** 65 passing | **Acceptance Criteria:** All met

#### Scope (Completed)
- Custom command/query dispatcher with FrozenDictionary O(1) lookup
- Sealed handler pattern (ICommand, IQuery generics with sealed handler interfaces)
- ValueTask<T> async returns for performance
- 4 Pipeline behaviors: Validation (FluentValidation), Logging (ILogger), Caching (INacCache), Transaction (IUnitOfWork)
- ISender dispatch interface
- Marker interfaces: ICacheableQuery, ITransactionalCommand
- Assembly scanning for automatic handler registration
- AddNacCqrs() DI extension method
- Full integration with Nac.Core dependency injection

#### Success Metrics (Achieved)
- ✅ All L1 CQRS tests passing (65 tests)
- ✅ O(1) handler dispatch performance via FrozenDictionary
- ✅ Full Nac.Core integration

---

### PDR 3: L1 Caching Package (COMPLETE ✅)

**Status:** ✅ Complete | **Wave:** 1  
**Tests:** 65 passing | **Acceptance Criteria:** All met

#### Scope (Completed)
- INacCache abstraction over HybridCache (.NET 10+)
- Tenant-aware key prefixing via ICurrentUser context
- Tag-based cache invalidation patterns
- CacheKey static utility for consistent key construction
- CacheEntryOptions configuration
- AddNacCaching() DI extension method
- Full Nac.Core integration

#### Success Metrics (Achieved)
- ✅ All L1 Caching tests passing (65 tests)
- ✅ HybridCache wrapper abstraction complete
- ✅ Tenant isolation in cache keys verified

---

### PDR 4: L2 Persistence Package (COMPLETE ✅)

**Status:** ✅ Complete | **Wave:** 1  
**Tests:** 65 passing | **Acceptance Criteria:** All met

#### Scope (Completed)
- NacDbContext abstract base (DB-agnostic EF Core 10)
- Repository<T> generic implementation with IRepository + IReadRepository
- Specification to IQueryable bridge (SpecificationExtensions)
- 4 EF Core Interceptors:
  - AuditableEntityInterceptor (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy)
  - SoftDeleteInterceptor (logical deletion support)
  - DomainEventInterceptor (event sourcing)
  - OutboxInterceptor (Outbox pattern)
- Transactional Outbox pattern with OutboxEvent model
- OutboxWorker BackgroundService for event processing
- AddNacPersistence<TContext>() DI extension method
- Full Nac.Core integration

#### Success Metrics (Achieved)
- ✅ All L2 Persistence tests passing (65 tests)
- ✅ Full EF Core 10 integration verified
- ✅ Outbox pattern implementation complete

---

### PDR 5: L2 Multi-Tenancy Package (COMPLETE ✅)

**Status:** ✅ Complete | **Wave:** 2B  
**Tests:** 75 passing | **Acceptance Criteria:** All met

#### Scope (Completed)
- TenantInfo and ITenantContext abstractions
- ITenantStore interface with 4 resolution strategies (Header, Claim, Route, Subdomain)
- TenantResolutionMiddleware for automatic tenant scoping
- MultiTenantDbContext with RLS query filters
- TenantEntityInterceptor for audit/soft-delete isolation
- ITenantConnectionStringResolver for per-tenant database factory
- AddNacMultiTenancy() and UseNacMultiTenancy() extensions
- Full Nac.Core integration

#### Success Metrics (Achieved)
- ✅ All L2 MultiTenancy tests passing (75 tests)
- ✅ Tenant context isolation verified across DB access patterns
- ✅ Migration strategies for single DB/schema/instance supported

---

### PDR 5A: L2 Tenant Management Package (COMPLETE ✅)

**Status:** ✅ Complete | **Wave:** 2B-Enhancement  
**Tests:** 38 passing | **Acceptance Criteria:** All met

#### Scope (Completed)
- Tenant aggregate (AggregateRoot<Guid>) with audit and soft-delete
- 5 domain events (Created, Updated, Deleted, Activated, Deactivated) implementing IIntegrationEvent
- TenantManagementDbContext (registry DB, non-multi-tenant)
- EfCoreTenantStore with 10-minute sliding cache override
- EncryptedConnectionStringResolver using Microsoft.AspNetCore.DataProtection
- 11 REST endpoints at `/api/admin/tenants` with policy + host-admin filter
- Bulk operations with best-effort failure tracking
- Outbox-emitted domain events
- AddNacTenantManagement(opts => opts.UseDbContext(...)) DI extension

#### Success Metrics (Achieved)
- ✅ All 38 unit tests passing
- ✅ Encryption round-trip verified
- ✅ Authorization enforced on all endpoints
- ✅ Outbox events emitted for all mutations

---

### PDR 6: L2 EventBus Package (COMPLETE ✅)

**Status:** ✅ Complete | **Wave:** 2A  
**Tests:** 80+ passing | **Acceptance Criteria:** All met

#### Scope (Completed)
- IEventPublisher, IEventHandler<T>, IEventDispatcher abstractions
- InMemoryEventBus with System.Threading.Channels transport
- EventHandlerRegistry with FrozenDictionary fan-out dispatch
- Outbox bridge (IIntegrationEventPublisher implementation)
- Assembly scanning for automatic handler registration
- AddNacEventBus() DI extension

#### Success Metrics (Achieved)
- ✅ All L2 EventBus tests passing (80+ tests)
- ✅ In-memory event publication and dispatch verified
- ✅ Outbox integration tested

---

### PDR 7: L2 Identity Package (COMPLETE ✅)

**Status:** ✅ Complete | **Wave:** 2B  
**Tests:** 89 passing | **Acceptance Criteria:** All met

#### Scope (Completed)
- NacUser extending IdentityUser<Guid>
- NacRole role management
- NacIdentityDbContext<TContext> for identity data
- CurrentUserAccessor implementing ICurrentUser from JWT
- IdentityService wrapping UserManager
- JwtTokenService for JWT token generation
- PermissionDefinitionManager with FrozenDictionary registry
- PermissionChecker with hierarchical rules
- PermissionAuthorizationHandler for ASP.NET Core Authorization
- AddNacIdentity<TContext>() DI extension

#### Success Metrics (Achieved)
- ✅ All L2 Identity tests passing (89 tests)
- ✅ Role and permission hierarchy verified
- ✅ JWT integration tested

---

### PDR 8: L2 Observability Package (COMPLETE ✅)

**Status:** ✅ Complete | **Wave:** 2C  
**Tests:** 28 passing | **Acceptance Criteria:** All met

#### Scope (Completed)
- LoggingEnricherMiddleware for HTTP context enrichment
- NacActivitySources OpenTelemetry constant definitions
- NacMeters for metrics collection
- NacLoggingScope fluent API for scoped logging context
- AddNacObservability() DI extension

#### Success Metrics (Achieved)
- ✅ All L2 Observability tests passing (28 tests)
- ✅ Structured logging enrichment verified

---

### PDR 9: L2 Jobs Package (COMPLETE ✅)

**Status:** ✅ Complete | **Wave:** 2C  
**Tests:** 21 passing | **Acceptance Criteria:** All met

#### Scope (Completed)
- IJobScheduler interface for one-time and delayed job enqueueing
- IRecurringJobManager interface for recurring job management
- IJobHandler<T> sealed handler pattern
- JobDefinition metadata class
- FakeJobScheduler and FakeRecurringJobManager test implementations
- AddNacJobs() DI extension

#### Success Metrics (Achieved)
- ✅ All L2 Jobs tests passing (21 tests)
- ✅ Abstract handler pattern tested with fakes

---

### PDR 10: L2 Testing Package (COMPLETE ✅)

**Status:** ✅ Complete | **Wave:** 2A  
**Tests:** 42 passing | **Acceptance Criteria:** All met

#### Scope (Completed)
- 7 in-memory fakes (FakeCurrentUser, FakeDateTimeProvider, FakePermissionChecker, FakeRepository<T>, FakeEventPublisher, FakeSender, FakeNacCache)
- TestEntityBuilder<T,B> abstract fluent builder
- ResultBuilder helper
- NacTestFixture for DI with fakes
- InMemoryDbContextFixture<T> for EF Core testing
- ResultAssertionExtensions for FluentAssertions
- AddNacTesting() DI extension

#### Success Metrics (Achieved)
- ✅ All L2 Testing tests passing (42 tests)
- ✅ Fake implementations verified across layers

---

### PDR 11: L3 WebApi Package (COMPLETE ✅)

**Status:** ✅ Complete | **Wave:** 3  
**Tests:** 42 passing | **Acceptance Criteria:** All met

#### Scope (Completed)
- NacWebApiModule composition root with module orchestration
- NacModuleLoader with Kahn's topological sort algorithm
- NacApplicationFactory with Pre/Config/Post lifecycle
- NacApplicationLifetime IHostedService
- 13-stage middleware pipeline with conditional inclusion
- NacExceptionHandler (RFC 9457 ProblemDetails)
- ResultToHttpMapper (6 ResultStatus → HTTP status codes)
- API versioning (Asp.Versioning 8.1.0)
- OpenAPI/Swagger integration
- CORS, rate limiting, compression, health checks

#### Success Metrics (Achieved)
- ✅ All L3 WebApi tests passing (42 tests)
- ✅ Module dependency resolution verified
- ✅ Middleware pipeline order and conditional logic tested

---

### PDR 12: Consumer Reference Architecture & Examples (COMPLETE ✅)

**Status:** ✅ Complete | **Wave:** 4A  
**Tests:** 11 integration tests passing | **Acceptance Criteria:** All met

#### Scope (Completed)
- **samples/ReferenceApp** — Orders + Billing multi-module blueprint
  - Orders module: aggregate, CQRS commands/queries, OrderCreatedEvent
  - Billing module: OrderCreatedEvent handler → automatic Invoice creation
  - Per-module DbContext with schema isolation
  - Cross-module event flow via Outbox
  - Integration tests with CRUD, permissions, JWT, multi-tenancy
  - External user-managed Postgres database (no docker-compose bundled)
- **NAC-Consumer-Project-Architecture.md** — Comprehensive consumer documentation

#### Success Metrics (Achieved)
- ✅ All 11 integration tests passing
- ✅ Multi-module event publishing and consumption verified
- ✅ Permission-based authorization tested

---

## Architecture Principles

### 1. Layered Dependency Graph
```
┌─────────┐
│  L3     │ WebApi (composition)
│ Nac.WebApi
└────┬────┘
     │
┌────▼─────────────────────────────────────────┐
│  L2                                           │
│  Nac.Persistence, Nac.Identity,              │
│  Nac.MultiTenancy, Nac.EventBus,             │
│  Nac.Jobs, Nac.Observability, Nac.Testing    │
└────┬──────────────────────────────────────────┘
     │
┌────▼──────────────────┐
│  L1                   │
│  Nac.Cqrs, Nac.Caching
└────┬─────────────────┤
     │                 │
┌────▼─────────────────▼──┐
│  L0 (Zero Dependencies)  │
│  Nac.Core               │
└─────────────────────────┘
```

**Rule:** Arrows point downward only. No circular dependencies.

### 2. Zero-Dependency L0
- L0 provides only interfaces and patterns
- No external NuGet packages (except MS.Extensions abstractions)
- All business logic is custom-built
- Maximum reusability across projects

### 3. Convention over Configuration
- DI marker interfaces enable auto-registration
- Entity/ValueObject base classes provide defaults
- Modularity attributes declare dependencies

### 4. SOLID Principles
- **S:** Each class has one reason to change
- **O:** Open for extension (Result pattern), closed for modification (Specification sealed)
- **L:** All implementations satisfy contracts
- **I:** Segregated interfaces (IRepository, IReadRepository)
- **D:** Dependency injection via abstractions

---

## Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Framework** | .NET LTS | 10.0 |
| **Language** | C# | 13.0+ |
| **Build** | MSBuild | Latest |
| **Testing** | xUnit | Latest |
| **Assertions** | FluentAssertions | Latest |
| **DI** | MS.Extensions.DependencyInjection | Latest abstractions |
| **Config** | MS.Extensions.Configuration | Latest abstractions |
| **Database** (L2) | Entity Framework Core | 10.0 |
| **Multi-Tenancy** (L2) | Finbuckle.MultiTenancy | Latest |
| **Event Bus** (L2) | RabbitMQ / Kafka (TBD) | TBD |
| **Logging** (L2) | Serilog | Latest |
| **Observability** (L2) | OpenTelemetry | Latest |
| **Jobs** (L2) | Hangfire | Latest |
| **Package Manager** | NuGet | Latest |
| **Source Control** | Git | GitHub |

---

## Release Strategy

### Version Scheme
- All packages version-locked to framework release
- Format: `{MajorVersion}.{MinorVersion}.{PatchVersion}`
- Example: `1.0.0`, `1.1.0`, `2.0.0-beta`

### Release Phases
1. **L0 (Current)** — Nac.Core v1.0.0 stable
2. **L1** — Nac.Cqrs, Nac.Caching v1.1.0
3. **L2** — Persistence, Identity, EventBus, etc. v1.2.0
4. **L3** — WebApi composition v1.3.0
5. **Templates & Examples** — v1.x.x

### Quality Gates
- 100% of tests passing (no skipped tests)
- 80%+ code coverage
- All public APIs documented
- No critical security issues
- Performance benchmarks within acceptable range

---

## Roadmap

| Phase | Deliverables | Timeline | Status |
|-------|-------------|----------|--------|
| **Phase 0** | Solution setup, Directory.Build.props, nuget.config | Completed | ✅ |
| **Phase 1** | Nac.Core (primitives, results, domain utilities) | Completed | ✅ |
| **Phase 2** | Nac.Core (DI, modularity, abstractions) | Completed | ✅ |
| **Phase 3** | Nac.Core unit tests (190 tests) | Completed | ✅ |
| **Wave 1** | L1 CQRS, Caching + L2 Persistence (255 tests total) | Completed | ✅ |
| **Phase 5** | Documentation updates (Wave 1 coverage) | In Progress | 🚀 |
| **Phase 6** | L2 Packages (Identity, Multi-Tenancy, EventBus, etc.) | Planned Q2-Q3 2026 | 📋 |
| **Phase 7** | L3 WebApi composition root | Planned Q3 2026 | 📋 |
| **Phase 8** | Templates and examples | Planned Q3-Q4 2026 | 📋 |

---

## Risks & Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|-----------|-----------|
| Scope creep (too many packages) | Schedule delay | Medium | Prioritize by adoption (CQRS, Persistence first) |
| Framework changes (.NET updates) | Compatibility issues | Low | Target LTS; plan upgrades quarterly |
| Circular dependencies (across layers) | Architecture failure | Medium | Enforce via unit tests; use architecture analyzer |
| Consumer adoption | Low usage | Medium | Provide reference apps; share templates early |
| Documentation lag | User frustration | High | Update docs immediately after code; automate |

---

## Success Criteria

### For PDR 1 (L0, COMPLETED)
- ✅ All 16 functional requirements implemented
- ✅ All 6 non-functional requirements met
- ✅ 190 unit tests passing
- ✅ Zero external dependencies (L0)
- ✅ Complete Nac.Core.csproj with proper metadata

### For Each Future PDR
- All functional requirements satisfied
- 80%+ test coverage
- All tests passing (no skipped/ignored)
- Public APIs documented
- Integration tests with L0
- README with usage examples
- Performance benchmarks (if applicable)

---

## Approval & Sign-Off

- **Project Lead:** Solo development (self-governed)
- **Framework Architecture:** Reviewed and approved per implementation phases
- **Quality Standard:** All tests pass before phase completion
- **Documentation:** Updated incrementally with each phase

---

**Last Updated:** 2026-04-16  
**Version:** 1.0 (L0 Complete)  
**Next Review:** After L1 packages complete
