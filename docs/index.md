# NAC Framework Documentation Index

Welcome to the NAC Framework documentation. Start here to navigate the entire documentation suite.

---

## Quick Navigation

### For New Developers
1. **[README.md](../README.md)** — Overview, quick start, CLI commands
2. **[Project Overview & PDR](./project-overview-pdr.md)** — Vision, goals, feature matrix
3. **[Code Standards](./code-standards.md)** — Naming conventions, patterns, best practices
4. **[Testing & Performance](./testing-and-performance.md)** — How to write tests and optimize queries

### For Architects
1. **[System Architecture](./system-architecture.md)** — CQRS pipelines, event systems, multi-tenancy
2. **[Project Overview & PDR](./project-overview-pdr.md)** — Design decisions, constraints
3. **[Codebase Summary](./codebase-summary.md)** — Package breakdown and responsibilities

### For Implementers
1. **[Code Standards](./code-standards.md)** — C# conventions, markers, patterns
2. **[Codebase Summary](./codebase-summary.md)** — What each package does
3. **[System Architecture](./system-architecture.md)** — How things fit together

### For Project Managers
1. **[Project Overview & PDR](./project-overview-pdr.md)** — Business goals, feature matrix
2. **[Project Roadmap](./project-roadmap.md)** — Release status, milestones, adoption targets
3. **[README.md](../README.md)** — Product positioning

---

## Documentation Files

| File | Lines | Purpose |
|------|-------|---------|
| **[README.md](../README.md)** | 476 | Project overview, quick start, CLI commands, examples |
| **[project-overview-pdr.md](./project-overview-pdr.md)** | 257 | Vision, goals, feature matrix, architectural decisions, success criteria |
| **[codebase-summary.md](./codebase-summary.md)** | 502 | Package-by-package breakdown with file lists, LOC, key types, versions |
| **[code-standards.md](./code-standards.md)** | 792 | Naming, C# 13 patterns, CQRS separation, entity design |
| **[testing-and-performance.md](./testing-and-performance.md)** | 268 | Unit/integration testing, performance optimization, caching, async patterns |
| **[system-architecture.md](./system-architecture.md)** | 945 | CQRS pipelines, dual events, persistence, multi-tenancy, caching, authorization |
| **[project-roadmap.md](./project-roadmap.md)** | 439 | v1.0 status, post-1.0 features, adoption targets, risk register |
| **index.md** | This file | Documentation navigation and overview |

**Total:** 3,679 LOC across 7 files. All under 800 LOC except system-architecture.md (see exception note below).

---

## Key Concepts at a Glance

### CQRS (Command Query Responsibility Segregation)
- **Commands** (write): Full pipeline with validation, authorization, transaction, audit
- **Queries** (read): Lightweight pipeline with validation, authorization, caching
- Separate handler interfaces prevent mixing concerns

### Module Architecture
- Each module owns its DbContext, endpoints, domain
- Modules communicate via Integration Events or Module Contracts
- Clear boundaries enable microservice extraction later

### Multi-Tenancy (Opt-in)
- Three isolation strategies: Discriminator, Schema-per-tenant, Database-per-tenant
- Five resolution strategies: Header, Claim, Subdomain, Query, Fallback
- Zero overhead when disabled

### Event System (Dual Layer)
- **Domain Events**: In-process, immediate, single module
- **Integration Events**: Distributed, async, Outbox pattern, cross-module

### Authorization (Permission-Based)
- Format: `module.resource.action` (e.g., `orders.create`, `catalog.*`)
- Wildcard support: `orders.*`, `*.approve`, etc.
- Tenant-scoped: permissions vary per tenant

---

## Architecture Stack

```
┌─────────────────────────────────────────┐
│         HTTP Request → Endpoint         │
├─────────────────────────────────────────┤
│    Mediator (CQRS)                      │
│  ├─ Command Pipeline                    │
│  │  ├─ Validation                       │
│  │  ├─ Authorization                    │
│  │  ├─ Transaction (UnitOfWork)         │
│  │  └─ Handler                          │
│  │     └─ Domain Event Dispatch         │
│  │                                      │
│  └─ Query Pipeline                      │
│     ├─ Validation                       │
│     ├─ Authorization                    │
│     ├─ Caching (check)                  │
│     └─ Handler                          │
├─────────────────────────────────────────┤
│    Data Access (EF Core)                │
│  ├─ Repository (no IQueryable)          │
│  ├─ Specification Pattern               │
│  └─ DbContext per Module                │
├─────────────────────────────────────────┤
│    Event Bus (Dual Layer)               │
│  ├─ Domain Events (in-process)          │
│  └─ Integration Events (distributed)    │
├─────────────────────────────────────────┤
│    Cross-cutting Concerns               │
│  ├─ Multi-tenancy                       │
│  ├─ Caching (with invalidation)         │
│  ├─ Logging (structured)                │
│  └─ Exception Handling                  │
└─────────────────────────────────────────┘
```

---

## 15 Packages Overview

| Tier | Package | Purpose |
|------|---------|---------|
| **Foundation** | Nac.Abstractions | Zero-dep interfaces (ICommand, IQuery, IRepository, etc.) |
|  | Nac.Domain | Entity, AggregateRoot, ValueObject, DomainEvent |
|  | Nac.Mediator | Custom CQRS mediator (no MediatR) |
| **Data** | Nac.Persistence | EF Core, UnitOfWork, Repository, Outbox |
|  | Nac.Persistence.PostgreSQL | PostgreSQL provider |
| **Messaging** | Nac.Messaging | EventBus abstraction, InMemory, Outbox |
|  | Nac.Messaging.RabbitMQ | RabbitMQ implementation |
| **Cross-cutting** | Nac.MultiTenancy | Tenant resolution, 3 strategies |
|  | Nac.Auth | Permission-based authorization |
|  | Nac.Caching | Query caching + invalidation |
|  | Nac.Observability | Structured logging |
| **API** | Nac.WebApi | Response envelopes, exception handler |
|  | Nac.Testing | Fakes (EventBus, TenantContext, User) |
| **Distribution** | Nac.Cli | dotnet CLI tool (`nac` commands) |
|  | Nac.Templates | `dotnet new nac-solution` template |

---

## Typical Development Workflow

```bash
# 1. Create new project
nac new MyApp --modules Catalog,Orders --db postgresql

# 2. Add a feature
nac add feature Catalog/CreateProduct

# 3. Implement handler, validator, endpoint
# (See Code Standards for patterns)

# 4. Create database migration
nac migration add Catalog "Add Products table"
nac migration apply

# 5. Run and test
dotnet run --project src/MyApp.Host

# 6. Verify architecture
nac check architecture
```

---

## Common Patterns & Examples

### 1. Create Command Handler
See [Code Standards → CQRS Separation](./code-standards.md#cqrs-separation)

### 2. Query with Caching
See [System Architecture → Caching Architecture](./system-architecture.md#caching-architecture)

### 3. Multi-Tenancy Setup
See [System Architecture → Multi-Tenancy Architecture](./system-architecture.md#multi-tenancy-architecture)

### 4. Permission-Based Authorization
See [System Architecture → Authorization Architecture](./system-architecture.md#authorization-architecture)

### 5. Integration Events
See [System Architecture → Dual Event System](./system-architecture.md#dual-event-system)

---

## Decision Reference

### "Should I use Domain or Integration Events?"
**Domain Events** for same-module consistency (within transaction).
**Integration Events** for cross-module communication (eventual consistency).
→ See [System Architecture → Dual Event System](./system-architecture.md#dual-event-system)

### "How do I share data between modules?"
1. **First choice:** Integration Events (async, loose coupling)
2. **Second choice:** Module Contract (sync, when you need immediate response)
3. **Last choice:** Shared Kernel (only for minimal, stable types)
→ See [System Architecture → Module Communication](./system-architecture.md#module-communication)

### "Do I need multi-tenancy?"
No—it's opt-in. Enable only if your product needs it.
When disabled: zero overhead, no query filters, no resolution middleware.
→ See [System Architecture → Multi-Tenancy Architecture](./system-architecture.md#multi-tenancy-architecture)

### "How do I scale from monolith to microservices?"
Modules already have clear boundaries.
Extraction is mechanical: extract DbContext, events, endpoints to separate service.
No architectural rewrite needed.
→ See [System Architecture → Deployment Architecture](./system-architecture.md#deployment-architecture)

---

## Roadmap Highlights

- ✅ **v1.0 (April 2026):** All 15 packages complete
- 🚀 **v1.1 (Q2 2026):** Kafka support, Saga pattern framework
- 📊 **v1.2 (Q3 2026):** OpenAPI/Swagger, GraphQL endpoint generator
- 📈 **v1.3 (Q4 2026):** Advanced observability, GDPR toolkit, Audit trail

See [Project Roadmap](./project-roadmap.md) for full timeline.

---

## Getting Help

- **How do I write a handler?** → [Code Standards](./code-standards.md)
- **How do I write tests?** → [Testing & Performance](./testing-and-performance.md)
- **How do I optimize queries?** → [Testing & Performance](./testing-and-performance.md#performance-considerations)
- **How does CQRS work?** → [System Architecture](./system-architecture.md)
- **What does package X do?** → [Codebase Summary](./codebase-summary.md)
- **What's the vision?** → [Project Overview & PDR](./project-overview-pdr.md)
- **What's coming next?** → [Project Roadmap](./project-roadmap.md)
- **How do I get started?** → [README.md](../README.md)

---

## File Size Management

Target: 800 LOC per file (enforced via guidelines).

```
code-standards.md          792 LOC  ████████████████████ 99%
system-architecture.md     945 LOC  ████████████████████ 118%† (exception)
testing-and-performance.md 268 LOC  ██████░░░░░░░░░░░░░░ 34%
project-roadmap.md         439 LOC  ███████████░░░░░░░░░ 55%
codebase-summary.md        502 LOC  ████████████░░░░░░░░ 63%
project-overview-pdr.md    257 LOC  ██████░░░░░░░░░░░░░░ 32%
README.md                  476 LOC  ███████████░░░░░░░░░ 60%
```

**LOC Exception Policy:**
- Default target: < 800 lines per doc
- Exception: system-architecture.md (945 LOC) contains critical CQRS pipelines and architectural diagrams that form the foundation of the framework. Cannot be meaningfully split without breaking conceptual unity.
- New files above 800 LOC must justify exception in comment
- Split strategy: When file approaches 800, extract new topic to separate document (e.g., testing-and-performance.md)

---

## Last Updated

April 12, 2026 | Version 1.0 Complete

