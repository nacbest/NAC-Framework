# NAC Framework — Project Overview & Product Development Requirements

## Executive Summary

**NAC Framework** is a modular .NET 10 foundation framework for backend Web API projects. It provides composable building blocks (auth, multi-tenancy, persistence, messaging, caching, observability) that projects combine based on requirements. The framework emphasizes modularity, opt-in features, zero overhead when unused, and a clear scaling path from monolith to microservices.

- **Target:** .NET 10, C# 13
- **Architecture:** Modular Clean Architecture + Vertical Slice
- **Distribution:** Local NuGet feed + CLI tool (`nac` dotnet command)
- **Status:** 15/15 packages complete

---

## Vision & Goals

### Vision
Create a reusable foundation that eliminates boilerplate, enforces architectural boundaries, and enables teams to build scalable backend services without being locked into rigid patterns.

### Goals

| Goal | Rationale |
|------|-----------|
| **Zero boilerplate** | CLI-driven scaffolding (`nac new`, `nac add`) replaces copy-paste |
| **Module-first** | Clear boundaries enable independent testing, scaling, and eventual microservice extraction |
| **Opt-in features** | Multi-tenancy, messaging, caching enabled only when needed—no overhead tax |
| **No third-party mediator** | Custom mediator ensures full pipeline control; no MediatR dependency |
| **Clear scaling path** | Monolith → Monolith with async → Microservices without rewrite |
| **CQRS separation** | Distinct command/query pipelines prevent mixing concerns |
| **Permission-based auth** | Flexible authorization with wildcard support (module.resource.action) |

---

## Solution Structure

### 15 NuGet Packages (single Nac.slnx solution)

#### Core Foundation
| Package | Purpose | Dependencies |
|---------|---------|--------------|
| **Nac.Core** | Base types (Entity, AggregateRoot, ValueObject), interfaces, markers | DI.Abstractions only |
| **Nac.Domain** | DomainEvent, persistence contracts (Nac.Domain.Persistence) | Nac.Core |
| **Nac.CQRS** | Custom CQRS mediator, ICommand/IQuery, behaviors, handler resolution | Nac.Core |

#### Persistence & Data
| Package | Purpose | Dependencies |
|---------|---------|--------------|
| **Nac.Persistence** | EF Core, UnitOfWork, Repository, Outbox/Inbox patterns | Nac.Core, Domain, CQRS |
| **Nac.Persistence.PostgreSQL** | PostgreSQL provider wrapper | Persistence |

#### Messaging & Events
| Package | Purpose | Dependencies |
|---------|---------|--------------|
| **Nac.Messaging** | IEventBus abstraction, InMemoryEventBus, Outbox/Inbox | Nac.Core, Persistence |
| **Nac.Messaging.RabbitMQ** | RabbitMQ IEventBus implementation | Messaging, RabbitMQ.Client 7.2.1 |

#### Identity & Authentication
| Package | Purpose | Dependencies |
|---------|---------|--------------|
| **Nac.Identity** | ASP.NET Core Identity + JWT + tenant roles/permissions | Nac.Core, CQRS, Persistence, MultiTenancy |

#### Cross-cutting Concerns
| Package | Purpose | Dependencies |
|---------|---------|--------------|
| **Nac.MultiTenancy** | Tenant resolution (Header/Claim/Subdomain/Query), 3 strategies | Nac.Core |
| **Nac.Caching** | Query cache + invalidation behaviors | Nac.Core, CQRS |
| **Nac.Observability** | Logging behaviors (command/query entry/exit/duration) | Nac.Core, CQRS |

#### API & Distribution
| Package | Purpose | Dependencies |
|---------|---------|--------------|
| **Nac.WebApi** | Response envelopes, global exception handler, module framework | Nac.Core |
| **Nac.Testing** | Fake implementations (EventBus, TenantContext, CurrentUser) | Nac.Core, CQRS |
| **Nac.Cli** | `nac` dotnet tool (scaffold, add modules/features) | System.CommandLine |
| **Nac.Templates** | `dotnet new nac-solution` template package | None |

### Dependency Flow (strict one-direction)

```
Nac.Core (DI.Abstractions only — includes Entity, AggregateRoot, ValueObject)
  ↑
Nac.Domain ← Nac.Core  (DomainEvent, Nac.Domain.Persistence interfaces)
Nac.CQRS ← Nac.Core   (ICommand, IQuery, IMediator, behaviors)
  ↑
Nac.Persistence ← Nac.Core, Domain, CQRS
  ↑
Nac.Identity ← Nac.Core, Persistence, CQRS, MultiTenancy
Nac.Messaging ← Nac.Core, Persistence
Nac.MultiTenancy ← Nac.Core
Nac.Caching ← Nac.Core, CQRS
Nac.Observability ← Nac.Core, CQRS
Nac.WebApi ← Nac.Core  (gains INacModule, NacFrameworkBuilder)
Nac.Testing ← Nac.Core, CQRS
Nac.Cli ← System.CommandLine
```

No circular dependencies. Framework verifies on startup.

---

## Key Architectural Decisions

### 1. Custom Mediator (No MediatR)

**Decision:** Build custom CQRS mediator instead of using MediatR.

**Rationale:**
- Full control over behavior pipeline order (explicit, not auto-discovered)
- Framework independence—core doesn't depend on third-party package
- Separate command/query pipelines prevent accidental mixing

**Tradeoff:** More code to maintain, but crucial for framework stability.

### 2. DbContext Per Module (Mandatory)

**Decision:** Each module owns its DbContext—no shared DbContext.

**Rationale:**
- Clear module boundaries
- Migration independence
- Ready for microservice extraction
- Multi-tenancy isolation at DbContext level

### 3. Dual Event System (Domain + Integration)

**Decision:** Two event buses:
- **Domain Events** (in-process Mediator Notifications)—immediate, same transaction
- **Integration Events** (IEventBus)—cross-module, distributed, with Outbox pattern

**Rationale:**
- Domain events for internal consistency within module
- Integration events for eventual consistency across modules
- Outbox guarantees at-least-once delivery
- Easy swap between InMemoryEventBus (dev) and distributed (RabbitMQ/Kafka)

### 4. Permission-Based Authorization

**Decision:** Use permission-based auth (module.resource.action) instead of roles.

**Rationale:**
- Flexible: roles = permission sets, configurable at runtime
- Wildcard support: `orders.*` grants all order permissions
- Tenant-scoped: permissions bound to tenant when multitenancy enabled

### 5. Multi-tenancy Opt-in with Zero Overhead

**Decision:** When disabled, `ITenantContext` exists but `IsMultiTenant = false`—no query filters, no overhead.

**Rationale:**
- Single-tenant projects pay no performance cost
- Feature toggleable without code changes
- Three strategies available: Discriminator, Schema-per-tenant, Database-per-tenant

### 6. Module Communication via Contracts Only

**Decision:** Modules cannot reference each other's projects; must use integration events or module contracts.

**Rationale:**
- Prevents implicit coupling
- Enables independent deployment
- Architecture checker (`nac check architecture`) verifies at startup

### 7. No IQueryable Exposure

**Decision:** Repositories return domain entities or complete result sets; queries use Specification pattern.

**Rationale:**
- Encapsulates data access logic
- Prevents data access logic creeping into handlers
- Specification pattern enables complex queries without exposing ORM details

---

## Codebase Statistics

| Metric | Value |
|--------|-------|
| Total LOC (C#, excl. obj/bin) | ~4,575 |
| Total files | ~100 .cs files |
| Packages | 15 |
| Target framework | net10.0 |
| Language version | C# 13 |

---

## Feature Matrix

| Feature | Status | Notes |
|---------|--------|-------|
| Core Mediator (CQRS) | ✅ Complete | Separate command/query pipelines |
| Domain & Aggregate Roots | ✅ Complete | Base classes + domain event support |
| Entity Framework Core integration | ✅ Complete | UnitOfWork, Repository, Specification |
| PostgreSQL provider | ✅ Complete | via Nac.Persistence.PostgreSQL |
| Multi-tenancy | ✅ Complete | 3 strategies, 5 resolvers, opt-in |
| Authorization (Permission-based) | ✅ Complete | Marker interface enforcement via behavior |
| Event Bus (In-memory) | ✅ Complete | InMemoryEventBus for development |
| RabbitMQ Integration | ✅ Complete | Outbox/Inbox pattern, automatic retries |
| Distributed Caching | ✅ Complete | Query-level caching with invalidation |
| Observability (Logging) | ✅ Complete | Entry/exit/duration behaviors |
| CLI Tool | ✅ Complete | Scaffold solution, add modules/features |
| Templates | ✅ Complete | `dotnet new nac-solution` |
| Testing Utilities | ✅ Complete | Fakes (EventBus, TenantContext, CurrentUser) |

---

## Non-Functional Requirements

| Requirement | Target | Notes |
|-------------|--------|-------|
| **Performance** | <100ms p99 per request (single-tenant, cached) | Depends on app logic |
| **Scalability** | Scale from monolith to microservices | Module boundary isolation |
| **Security** | No secrets in code; audit trail support | Permission model, soft-delete |
| **Observability** | Structured logging + correlation IDs | OpenTelemetry-ready |
| **Reliability** | At-least-once event delivery | Outbox pattern in Messaging |
| **Testability** | Per-module isolation + fakes | NacTestHost, FakeEventBus |
| **Deployability** | Independent module/feature deployment | Module version tracking in nac.json |

---

## Constraints & Assumptions

### Constraints
- .NET 10+ only (no legacy frameworks)
- EF Core only for ORM (no Dapper, NHibernate)
- PostgreSQL recommended (others via custom provider)
- Single solution file (Nac.slnx) for all packages—no separate repos

### Assumptions
- Teams use Git for version control
- Projects run on Linux/Windows/macOS
- Docker deployment (implied)
- Async-first design throughout

---

## Success Criteria

| Criterion | Measurement |
|-----------|-------------|
| **Time to new project** | < 5 min with `nac new` + module setup |
| **Boilerplate reduction** | > 70% vs. manual setup |
| **Scaling readiness** | Module extraction to microservice = 1-2 day effort |
| **Developer adoption** | Team comfort with CLI commands within 1 week |
| **Defect injection** | Framework-enforced patterns reduce arch violations |

---

## Roadmap & Future

See **[Project Roadmap](./project-roadmap.md)** for complete post-v1.0 timeline, feature details, adoption targets, and risk register.

**Current Status (v1.0 — April 2026):**
- ✅ All 15 packages implemented
- ✅ CLI scaffolding complete
- ✅ Multi-tenancy strategies complete
- ✅ RabbitMQ messaging complete

---

## Documentation Index

- [Codebase Summary](./codebase-summary.md) — Package-by-package breakdown
- [Code Standards](./code-standards.md) — Naming, patterns, conventions
- [System Architecture](./system-architecture.md) — Diagrams, data flow, pipelines
- [Project Roadmap](./project-roadmap.md) — Release timeline, feature priorities

