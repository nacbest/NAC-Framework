# NAC Framework — Development Roadmap

Strategic roadmap for NAC Framework development through Q4 2026 and beyond.

---

## Current State

**Completed:** L0 Nac.Core + Wave 1 (L1 CQRS/Caching + L2 Persistence) + Wave 2A (Nac.EventBus, Nac.Testing) + Wave 2B (Nac.Identity, Nac.MultiTenancy) + Wave 2B-Enhancement (Nac.MultiTenancy.Management) + Wave 2C (Nac.Observability, Nac.Jobs) + Wave 3 (L3 Nac.WebApi) + Wave 4A (Consumer Reference Architecture) + **Pattern A Identity Migration v3.0** (Phases 01–09: Domain, Services, Auth, Roles, Admin, Onboarding, Cleanup, Tests, Docs)  
**Tests:** 626 unit tests + 11 integration tests, all passing  
**Packages:** 13 (Nac.Core, Nac.Cqrs, Nac.Caching, Nac.Persistence, Nac.EventBus, Nac.Testing, Nac.MultiTenancy, Nac.MultiTenancy.Management, Nac.Identity, **Nac.Identity.Management**, Nac.Observability, Nac.Jobs, Nac.WebApi)

---

## Wave 2: L2 Feature Layers (Q2-Q3 2026)

### Phase 6A: L2 EventBus & Testing (COMPLETE ✅)

**Completed (2026-04-16):**
1. ✅ **Nac.EventBus** — IEventPublisher/IEventHandler<T>/IEventDispatcher, InMemory transport (Channels), Outbox bridge, assembly scanning, fan-out dispatch
2. ✅ **Nac.Testing** — 7 in-memory fakes, fluent builders, NacTestFixture, InMemoryDbContextFixture<T>, assertion extensions

**Tests Added:** 80+ unit tests, all passing

### Phase 6B: L2 Identity & MultiTenancy (COMPLETE ✅)

**Completed (2026-04-16):**
1. ✅ **Nac.Identity** — ASP.NET Core Identity wrapper, JWT generation, ICurrentUser impl, IPermissionChecker with role/permission hierarchy
2. ✅ **Nac.MultiTenancy** — ITenantContext/ITenantStore abstractions, 4 resolution strategies (Header/Claim/Route/Subdomain), query filters, per-tenant DB factory

**Tests Added:** 150+ unit tests, all passing

### Phase 6C: L2 Observability & Jobs (COMPLETE ✅)

**Completed (2026-04-16):**
1. ✅ **Nac.Observability** — LoggingEnricherMiddleware, NacActivitySources, NacMeters, NacLoggingScope
2. ✅ **Nac.Jobs** — IJobScheduler, IRecurringJobManager, IJobHandler<T>, JobDefinition, FakeJobScheduler, FakeRecurringJobManager fakes

**Tests Added:** 49 unit tests (28 Observability + 21 Jobs), all passing

### Phase 7: L3 Composition Root (COMPLETE ✅)

**Completed (2026-04-17):**
1. ✅ **Nac.WebApi** — Composition root with module system orchestration
   - NacModuleLoader with Kahn's topological sort algorithm
   - NacApplicationFactory with Pre/Config/Post lifecycle
   - NacApplicationLifetime IHostedService for Init/Shutdown
   - Middleware pipeline with 13 stages, conditional includes
   - NacExceptionHandler (RFC 9457 ProblemDetails)
   - ResultToHttpMapper (6 ResultStatus → HTTP)
   - API versioning (Asp.Versioning 8.1.0)
   - OpenAPI/Swagger integration
   - NacWebApiOptions feature toggles
   - Consumer API: 4 lines in Program.cs

**Tests Added:** 42 unit tests, all passing

**Total:** 577 tests (all passing)

---

## Wave 4: Consumer Reference Architecture & Enhancements (2026-Q3)

### Phase 8A: Consumer Reference Architecture (COMPLETE ✅)

**Completed (2026-04-17):**
1. ✅ **samples/ReferenceApp** — Orders + Billing modular blueprint with cross-module event flow
   - Orders module: aggregate, CQRS, OrderCreatedEvent publication via Outbox
   - Billing module: OrderCreatedEvent handler → automatic Invoice creation
   - Integration tests: 11 passing (CRUD, permissions, multi-tenancy, JWT, cross-module events)
   - Per-module DbContext with schema isolation (external user-managed Postgres; no docker-compose bundled)
2. ✅ **Framework fix: AddNacEventBus idempotency** — Safe multi-module registration, shared channel architecture
3. ✅ **Consumer doc rewrite:** NAC-Consumer-Project-Architecture.md (938→972 LOC, 17 sections, real API surface)

**Tests Added:** 11 integration tests, all passing

### Phase 8A-Ext: Tenant Management Module (COMPLETE ✅)

**Completed (2026-04-19):**
1. ✅ **Nac.MultiTenancy.Management** — Admin-facing tenant lifecycle on top of Nac.MultiTenancy
   - Tenant aggregate (AggregateRoot<Guid>) with audit + soft-delete
   - 5 domain events emitted to Outbox (Created, Updated, Deleted, Activated, Deactivated)
   - TenantManagementDbContext (centralized registry, not multi-tenant)
   - EfCoreTenantStore override with 10-minute sliding cache
   - EncryptedConnectionStringResolver using Microsoft.AspNetCore.DataProtection
   - 11 REST endpoints (/api/admin/tenants) with host-admin authorization
   - Bulk operations (activate, deactivate, delete) with 207 Multi-Status support
   - DI extension: AddNacTenantManagement(opts => opts.UseDbContext(...))
2. ✅ **Module README:** Installation, quickstart, API reference, DataProtection key persistence guidance
3. ✅ **Framework docs updated:** project-overview-pdr.md, codebase-summary.md, system-architecture.md, project-changelog.md, project-roadmap.md

**Tests Added:** 38 unit tests, all passing

**Key Design Decisions:**
- Registry DB separate from multi-tenant application DBs (centralized tenant metadata)
- Encryption at rest via DataProtection (purpose: `Nac.MultiTenancy.Management.ConnectionString`)
- Cache invalidation on all mutations; manual API via ITenantCacheInvalidator
- Host-realm only (non-null TenantId rejected); ICurrentUser enforcement
- Outbox integration for reliable domain event publication

### Phase 8A-Ext2: Pattern A Identity Migration v3.0 (COMPLETE ✅)

**Completed (2026-04-20):**
1. ✅ **Phases 01–07 Implementation** — Domain refactor, services, auth endpoints, role templates, membership services, tenant switching, admin endpoints, host permissions
   - New files: `HostPermissions.cs`, `HostPermissionProvider.cs`, `HostQueryExtensions.cs`
   - Framework enhancement: `ForbiddenAccessException.cs` in Nac.Core/Domain
   - Middleware auto-registration: `TenantRequiredGateMiddleware` in `UseNacApplication`
   - Authorization: `HostAdminOnlyFilter` checks both `IsHost` flag AND `Host.AccessAllTenants` permission
   - Pattern A finalized: Global users, tenant-scoped memberships, runtime permission resolution
2. ✅ **JWT Shape Finalized:** `sub, email, name?, tenant_id?, role_ids?, is_host?` (no permission claims)
3. ✅ **Permission Resolution:** Cache-backed store with 10-minute TTL; invalidated on role/grant changes
4. ✅ **Phase 08 Tests:** 164 identity tests (150 unit + 14 Postgres integration) covering R1 isolation, R3 instant revoke, template seeder, onboarding flow, permission E2E
5. ✅ **Phase 09 Docs:** `docs/identity-and-rbac.md` (12 sections incl. Customer Identity Pattern Guide), v3.0.0 changelog, roadmap update, README quickstart

**Tests Added:** 164 identity tests + existing suite, all passing

**Key Decisions:**
- `NacUser` has no `TenantId` — users are global identities; memberships define tenant scope
- Permissions evaluated at request time, not embedded in JWT (enables live updates)
- Host realm (`IsHost=true`) users access all tenants via `Host.AccessAllTenants` permission
- `TenantRequiredGateMiddleware` auto-gates tenant-scoped endpoints (403 if tenant null)
- v3 role-template clones are **immutable** — template-sync deferred to v4

**Follow-ups (v4):**
- Deny-list grants (allow-only today)
- Resource-aware permission grants (`IsGrantedAsync(perm, resourceType, resourceId)` stubbed)
- Template-sync flow (propagate template edits to tenant clones)
- Refresh token rotation (`/auth/refresh` 501 stub today)
- Customer stack starter repo (beyond the docs recipe)

### Phase 8B: Framework Enhancements (Pending)

**Scope:**

#### Enhancement 1: OutboxWorker<TContext> Genericization
- Root cause: Current OutboxWorker resolves single NacDbContext alias (last-registration-wins)
- Impact: Multi-module solutions with multiple DbContexts need workaround
- Fix: Generic `OutboxWorker<TContext>` paired with distinct outbox polling per context
- Target: v1.6.0 (Q3 2026)

#### dotnet new Templates (Deferred to v2.0.0)
1. **nac-solution** — Full solution scaffolding with all L0-L3 packages
2. **nac-module** — Domain module with DomainService pattern
3. **nac-entity** — Domain entity with AggregateRoot boilerplate
4. **nac-endpoint** — API endpoint with CQRS handler

#### Reference Examples
1. **SimpleCrud** — Basic CRUD app (User + Profile entities)
   - ~300 LOC
   - Demonstrates: Result pattern, Spec queries, basic CQRS
   - No multi-tenancy or complex auth

2. **SaaSStarter** — Multi-tenant SaaS application
   - ~2000 LOC
   - Demonstrates: Full L0-L3 stack, multi-tenancy, identity, observability
   - Real-world patterns: user onboarding, role-based access, audit logs
   - Includes migration scripts, seed data

3. **MicroserviceExtract** — Module extraction to standalone service
   - ~1500 LOC
   - Demonstrates: Breaking monolith module into microservice
   - Service-to-service communication patterns
   - Event-driven architecture

**Target Completion:** Q4 2026

**Success Metrics:**
- All templates installable via `dotnet new nac-*`
- Examples compile and run without errors
- Documentation examples reflect real-world scenarios
- Community feedback incorporated

---

## Post-Launch: Maintenance & Enhancement (2027+)

### Adoption & Feedback Loop
- NuGet package downloads tracked
- Community issues/PRs processed weekly
- Quarterly release cadence
- Security patch policy: within 48 hours

### Planned Enhancements (TBD)
- Distributed transaction support (Saga pattern)
- GraphQL CQRS integration
- gRPC support
- Dapr integration
- Cloud-native resilience patterns (circuit breaker, retry, bulkhead)
- Performance optimizations based on benchmarks

---

## Risk Assessment & Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|-----------|-----------|
| Scope creep (too many packages) | Schedule slip | Medium | Prioritize MVP packages, defer nice-to-haves |
| Circular dependencies (L2+) | Architecture fail | Medium | Enforce via unit tests; use static analyzer |
| Performance regression | Adoption barrier | Low | Benchmark baseline before each release |
| Community adoption low | Project sustainability | Medium | Share templates early; create video tutorials |
| Breaking changes needed | Consumer pain | Low | Deprecation warnings; 2 major versions support |
| Third-party lib updates | Compatibility issues | Low | Pin major versions; quarterly update review |

---

## Quality Gates (All Waves)

✅ **Mandatory for Release:**
- 80%+ code coverage (minimum)
- 100% of tests passing (no skipped)
- All public APIs documented (XML comments)
- No critical security issues (OWASP Top 10)
- Performance benchmarks within expected range
- Backward compatibility verified (if applicable)

✅ **Code Review Process:**
- Lead architecture review (layering, dependencies)
- Peer code review (2+ reviewers)
- Automated testing (xUnit + static analysis)
- Performance profiling (critical paths)

---

## Documentation Plan

**By Phase:**
- Phase 6 (L2): API reference for each new package
- Phase 7 (L3): Integration guide, architecture decision records
- Phase 8 (Examples): Step-by-step tutorials, video walkthroughs

**Maintained Docs:**
- system-architecture.md — Updated per phase
- codebase-summary.md — Updated per phase
- project-overview-pdr.md — PDR for each new package
- project-changelog.md — Release notes per version
- project-roadmap.md — This file, quarterly updates

---

## Success Criteria

### Immediate (By Q2 2026) ✅ ACHIEVED
- [x] EventBus & Testing packages complete (Wave 2A)
- [x] L2 Identity & MultiTenancy complete (Wave 2B)
- [x] L2 Observability & Jobs complete (Wave 2C)
- [x] L3 WebApi composition root complete (Wave 3)
- [x] 40+ integration tests for L3
- [ ] All examples compile and run (Wave 4)

### Long-term (By Q4 2026)
- [ ] All templates installable via dotnet new
- [ ] 100+ GitHub stars
- [ ] First community contributor PR
- [ ] NuGet downloads > 1000/month
- [ ] Zero critical security issues
- [ ] Performance benchmarks published

### Sustainability (2027+)
- [ ] Quarterly release cadence maintained
- [ ] <48 hour response time for critical issues
- [ ] Community contributions reviewed weekly
- [ ] Documentation kept current with code

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
| 1.5.1 | 2026-04-17 | Consumer Reference Architecture (samples/ReferenceApp, EventBus idempotency fix) | ✅ Complete |
| 1.6.0 | 2026-04-19 | Pattern A Identity Migration (Phases 01–07: Domain, Services, Auth, Roles, Membership, Admin, Host Permissions) | ✅ Complete |
| 3.0.0 | 2026-04-20 | Pattern A Identity Migration v3.0 (Phases 08–09: Tests 164, Docs incl. Customer Identity Guide, `Nac.Identity.Management` package) | ✅ Complete |
| 1.7.0 | 2026-Q3 | OutboxWorker<TContext> enhancement, multi-module outbox polling | 📋 Planned |
| 2.0.0 | 2026-Q4 | dotnet new templates, Examples expansion | 📋 Planned |

---

## Technology Decisions (Subject to Review)

| Decision | Rationale | Alternative |
|----------|-----------|-------------|
| FrozenDictionary for CQRS | O(1) dispatch performance | Dictionary + lock or ConcurrentDictionary |
| HybridCache over DistributedCache | Single API for hybrid caching | Separate in-mem and distributed caches |
| EF Core for ORM | .NET-native, LINQ support | Dapper, NHibernate |
| FluentValidation over DataAnnotations | Composable, testable validation | DataAnnotations attributes |
| Finbuckle for multi-tenancy | Proven, feature-rich | Custom implementation |
| xUnit over NUnit | Modern, better async support | NUnit, MSTest |

---

## Quarterly Reviews

Roadmap reviewed quarterly (Jan, Apr, Jul, Oct):
- Progress against milestones
- Risk assessment updates
- Scope adjustments based on adoption feedback
- Timeline revisions if needed

**Next Review:** Q3 2026 (July)

---

**Last Updated:** 2026-04-20 (Pattern A Identity Migration v3.0 complete — Phases 01–09 incl. 164 tests + docs/identity-and-rbac.md)  
**Next Update:** 2026-07-19  
**Maintainer:** Solo development  
**License:** MIT
