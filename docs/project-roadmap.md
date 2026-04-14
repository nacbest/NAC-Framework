# NAC Framework — Project Roadmap

Current status, milestones, and future development priorities.

---

## Release Status

### v1.0 — Foundation Complete ✅

**Release Date:** April 2026

**Status:** All 15 packages implemented and tested.

#### Packages (15/15)

| Package | Status | Version | Notes |
|---------|--------|---------|-------|
| Nac.Abstractions | ✅ Complete | 1.0.0 | Zero dependencies |
| Nac.Domain | ✅ Complete | 1.0.0 | Entity, AggregateRoot, ValueObject |
| Nac.Mediator | ✅ Complete | 1.0.0 | Custom CQRS mediator, no MediatR |
| Nac.Persistence | ✅ Complete | 1.0.0 | EF Core, UoW, Repository, Outbox |
| Nac.Persistence.PostgreSQL | ✅ Complete | 1.0.0 | PostgreSQL provider wrapper |
| Nac.Messaging | ✅ Complete | 1.0.0 | IEventBus, InMemory, Outbox |
| Nac.Messaging.RabbitMQ | ✅ Complete | 1.0.0 | RabbitMQ integration |
| Nac.MultiTenancy | ✅ Complete | 1.0.0 | 3 strategies, 5 resolvers |
| Nac.Caching | ✅ Complete | 1.0.0 | Query-level caching + invalidation |
| Nac.Observability | ✅ Complete | 1.0.0 | Structured logging |
| Nac.WebApi | ✅ Complete | 1.0.0 | Response envelopes, exception handler |
| Nac.Testing | ✅ Complete | 1.0.0 | Fakes (EventBus, TenantContext, User) |
| Nac.Cli | ✅ Complete | 1.0.0 | CLI tool (`nac` commands) |
| Nac.Templates | ✅ Complete | 1.0.0 | `dotnet new nac-solution` |

#### Feature Completeness (v1.0)

| Feature | Status | Details |
|---------|--------|---------|
| **CQRS Mediator** | ✅ | Separate command/query pipelines, behaviors |
| **Domain Model** | ✅ | AggregateRoot, Entity, ValueObject, DomainEvent |
| **EF Core Integration** | ✅ | DbContext per module, UnitOfWork, Repository |
| **PostgreSQL Support** | ✅ | Nac.Persistence.PostgreSQL package |
| **Multi-tenancy** | ✅ | Discriminator, Schema-per-tenant, Database-per-tenant |
| **Tenant Resolution** | ✅ | Header, Claim, Subdomain, Query, Fallback |
| **Authorization** | ✅ | Permission-based, wildcard support, tenant-scoped |
| **Event Bus (In-Memory)** | ✅ | InMemoryEventBus for development |
| **Event Bus (RabbitMQ)** | ✅ | Outbox pattern, retry, idempotency |
| **Distributed Caching** | ✅ | IDistributedCache abstraction, per-query TTL |
| **Cache Invalidation** | ✅ | Post-command invalidation, pattern support |
| **Observability** | ✅ | Structured logging, correlation IDs |
| **API Response Envelope** | ✅ | ApiResponse<T>, PaginatedResponse<T> |
| **Exception Handling** | ✅ | Global handler, HTTP status mapping |
| **CLI Scaffolding** | ✅ | `nac new`, `nac add module/feature/entity` |
| **Templates** | ✅ | `dotnet new nac-solution` |
| **Testing Utilities** | ✅ | FakeEventBus, FakeTenantContext, FakeCurrentUser |

---

## Post-v1.0 Roadmap

### Q2 2026: Enhancements & Polish

#### Priority 1: Kafka Support (High)

**Package:** `Nac.Messaging.Kafka`

**Rationale:** Teams using Kafka have no messaging option; RabbitMQ alone insufficient.

**Scope:**
- `KafkaEventBus` implementing `IEventBus`
- Consumer group management
- Partition selection strategy
- Exactly-once semantics option
- Configuration via `AddKafkaEventBus()`

**Effort:** 2 weeks | **Dependencies:** Confluent.Kafka NuGet

---

#### Priority 2: Saga Pattern Framework (High)

**Package:** `Nac.Sagas`

**Rationale:** Multi-step workflows (Order → Inventory Deduction → Payment) need coordinated failure handling without explicit orchestration.

**Scope:**
- `ISagaDefinition<TState>` base interface
- State machine (`Pending → Approved → Shipped → Completed`)
- Compensation logic for rollback
- Timeout handling
- Saga state persistence

**Example Use Case:**
```csharp
// Order saga: wait for inventory, then payment, then shipment
public sealed class OrderSagaDefinition : ISagaDefinition<OrderSagaState>
{
    public void Define(ISagaBuilder<OrderSagaState> builder)
    {
        builder
            .Event<InventoryReservedIntegrationEvent>()
            .Then(state => new RequestPaymentCommand(state.OrderId, state.Amount))
            .Compensate(state => new ReleaseInventoryCommand(state.OrderId))
            .Next(OrderSagaStep.AwaitingPayment);
    }
}
```

**Effort:** 3 weeks | **Dependencies:** State machine concept

---

### Q3 2026: API & Documentation

#### Priority 3: OpenAPI/Swagger Integration (Medium)

**Package:** `Nac.OpenApi`

**Rationale:** Auto-generate Swagger from mediator handlers + endpoint definitions.

**Scope:**
- Auto-discover commands/queries from handlers
- Generate OpenAPI spec from command/query types
- Per-module Swagger UI
- Request/response schema inference
- Validation rule annotation

**Result:** `GET /api/swagger/catalog` returns OpenAPI spec.

**Effort:** 2 weeks | **Dependencies:** Swashbuckle or custom implementation

---

#### Priority 4: GraphQL Endpoint Generator (Medium)

**Package:** `Nac.GraphQL`

**Rationale:** REST + GraphQL from same mediator backend.

**Scope:**
- Auto-generate GraphQL schema from `IQuery<T>` types
- Query endpoint: `POST /graphql` with mediator dispatch
- Per-tenant schema (if multitenancy enabled)
- Subscription support (via SignalR + events)

**Result:** Single mediator, multiple API styles (REST + GraphQL).

**Effort:** 3 weeks | **Dependencies:** HotChocolate or GraphQL-dotnet

---

### Q4 2026: Observability & DevOps

#### Priority 5: Advanced Observability (Medium)

**Package:** `Nac.Observability.OpenTelemetry`

**Rationale:** Structured logging alone insufficient for microservices; need metrics + tracing.

**Scope:**
- OpenTelemetry SDK integration
- Per-command/query metrics (count, duration, errors)
- Distributed tracing (parent-child span propagation)
- Custom meter for business events
- Integration with OTEL Collector

**Metrics Collected:**
- `command.execution_count` (labeled by command type)
- `command.duration_ms` (histogram)
- `query.cache_hits_ratio`
- `event.published_count` (labeled by event type)

**Effort:** 2 weeks | **Dependencies:** OpenTelemetry NuGet

---

#### Priority 6: Distributed Cache (Redis) Pre-configuration (Low)

**Package:** `Nac.Caching.Redis` (convenience, not required)

**Rationale:** Simplify Redis setup for projects using distributed cache.

**Scope:**
- Fluent method: `.UseRedisCache(opts => opts.Connection = "...")`
- Connection pooling preconfigured
- Automatic serialization (MessagePack or Protobuf)
- Health check integration

**Effort:** 1 week | **Dependencies:** StackExchange.Redis

---

### Q1 2027: Enterprise Features

#### Priority 7: Audit Trail Framework (High)

**Package:** `Nac.Auditing`

**Rationale:** Compliance (GDPR, SOX) requires immutable audit logs.

**Scope:**
- Opt-in via `IAuditableCommand` marker
- Automatic logging: who, what, when, tenant, before/after values
- Audit event storage (custom handler or built-in table)
- Query audit history: `GetAuditTrailQuery<TEntity>(Guid id)`
- Retention policies (auto-delete after N days per GDPR)

**Example:**
```csharp
public sealed record UpdateProductPriceCommand(Guid Id, decimal NewPrice)
    : ICommand,
      IAuditableCommand  // Triggers audit logging
{
}

// Handler updates Product.Price
// Audit trail captures: "Price changed from $100 to $150 by user:alice in tenant:acme"
```

**Effort:** 2 weeks | **Dependencies:** Custom implementation

---

#### Priority 8: GDPR Toolkit (Medium)

**Package:** `Nac.Gdpr`

**Rationale:** Data deletion, anonymization, export workflows.

**Scope:**
- Right-to-deletion flow (event-driven cleanup)
- Anonymization (replace PII with `***`)
- Data export (per user, per tenant)
- Audit compliance (no hard deletes, only soft)
- Automated retention policies

**Commands:**
```csharp
public sealed record DeleteUserDataCommand(Guid UserId, Guid TenantId) 
    : ICommand;  // Triggers cascade deletion in all modules

public sealed record ExportUserDataCommand(Guid UserId) 
    : ICommand<StreamContent>;  // Returns JSON export
```

**Effort:** 3 weeks | **Dependencies:** Soft-delete, events

---

### Future Considerations (Q2+ 2027)

#### Potential Features (Under Evaluation)

| Feature | Rationale | Status |
|---------|-----------|--------|
| **Feature Flags** | Enable/disable features per tenant | Research phase |
| **A/B Testing** | Experiment management framework | Backlog |
| **Cost Accounting** | Per-tenant usage tracking + billing | Backlog |
| **Database Multi-master Replication** | High-availability databases | Backlog |
| **Service Mesh Integration** | Kubernetes/Istio patterns | Backlog |
| **Event Sourcing** (opt-in) | Immutable event store + event replay | Backlog |
| **Temporal.io Integration** | Complex workflows (long-running processes) | Backlog |
| **Business Rules Engine** | Decoupled business rule evaluation | Backlog |

---

## Development Velocity & Metrics

### Completed (v1.0)

| Metric | Value |
|--------|-------|
| **Total LOC** | 4,575 |
| **Total Packages** | 15 |
| **Avg Package Size** | 305 LOC |
| **Development Time** | ~4 weeks (implementation) |
| **Code Review Cycles** | 3 full reviews |
| **Test Coverage** | 70%+ (target) |

### Quality Gates (Post-v1.0)

All future packages must meet:
- **Test Coverage:** ≥ 70%
- **Code Review:** 2 approvals
- **Architecture Tests:** No boundary violations
- **Performance:** No regression > 5% vs. baseline
- **Security:** No critical/high findings

---

## Success Criteria per Release

### v1.0 (April 2026)

- ✅ 15 packages complete
- ✅ CLI scaffolding works (tested with 3 real projects)
- ✅ Multi-tenancy strategies validated
- ✅ RabbitMQ integration stable
- ✅ Documentation complete

### v1.1 (Q2 2026)

- [ ] Kafka support stable
- [ ] Saga pattern widely adopted
- [ ] OpenAPI generation reduces manual documentation
- [ ] 0 critical issues from v1.0
- [ ] Team adopts NAC for 2+ new projects

### v1.2+ (Q3-Q4 2026)

- [ ] Distributed observability in prod
- [ ] GDPR toolkit deployed in at least 1 project
- [ ] Audit trail captures all compliance requirements
- [ ] Framework scales to 50+ microservices
- [ ] Community PRs for feature extensions

---

## Adoption Targets

### Short Term (6 months)

- **Internal:** 3-4 new projects using NAC
- **External:** Open-source release (GitHub)
- **Documentation:** Full coverage with 50+ examples

### Medium Term (12 months)

- **Community:** 10+ community-contributed features
- **Ecosystem:** 3+ complementary packages (distributed transactions, event sourcing, etc.)
- **Maturity:** v2.0 with breaking changes finalized

### Long Term (18+ months)

- **Adoption:** Benchmark against other frameworks (Clean Architecture kits, Vertical Slice Template)
- **Certification:** Developer certification program
- **Commercial:** Optional enterprise support tier

---

## Breaking Changes Policy

### Semantic Versioning
- **v1.x:** API stable; breaking changes only in v2
- **v2+:** Semantic versioning respected

### Deprecation Warning Period
- New major version: 6-month deprecation window
- Migration guide + code examples provided
- Automated tooling (`nac upgrade`) assists migration

---

## Dependency Update Strategy

### Framework Dependencies

| Package | Current | Update Policy |
|---------|---------|----------------|
| .NET | 10.0 | Track LTS (n+2 years support) |
| EF Core | 10.0.5 | Update with .NET |
| RabbitMQ.Client | 7.2.1 | Quarterly minor updates |
| System.CommandLine | 2.0.5 | As available |
| Npgsql | Latest | Quarterly updates |

### Security Updates
- Critical: Same-day patch release
- High: Weekly release cycle
- Medium: Monthly release cycle

---

## Monitoring & Feedback

### Metrics Tracked

- **Adoption:** # of projects using NAC
- **Engagement:** GitHub stars, forum activity
- **Performance:** Benchmark comparisons
- **Quality:** Issue resolution time, bug count

### Feedback Channels

- GitHub Issues (bug reports, feature requests)
- Discussions forum (architecture questions)
- Monthly sync with early adopters
- Annual summit (Q1) for community feedback

---

## Dependencies & Constraints

### External Dependencies

- **.NET 10+:** Non-negotiable
- **EF Core:** Only supported ORM (no Dapper, NHibernate)
- **PostgreSQL:** Recommended; others via custom provider
- **RabbitMQ/Kafka:** Optional for distributed messaging

### Organizational Constraints

- **Single solution file:** All packages in Nac.slnx
- **CLI-first:** Scaffolding is primary workflow, not secondary
- **Module isolation:** Non-negotiable; enforced at build time
- **Zero third-party mediator:** Custom implementation mandatory

---

## Risk Register

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| Breaking changes in .NET 11 | Medium | High | Monitor releases early, participate in betas |
| Adoption slower than expected | Medium | Medium | Invest in documentation, examples, webinars |
| Community fragmentation (forks) | Low | High | Clear governance, responsive maintainers |
| Performance regression in scaling | Low | High | Continuous benchmarking, load tests |
| Security vulnerability in dependency | Low | High | Security scanning, rapid patch cycle |

---

## Glossary & Links

- **CQRS:** Command Query Responsibility Segregation
- **DDD:** Domain-Driven Design
- **Saga:** Multi-step workflow with compensation
- **Outbox Pattern:** Ensures reliable event publishing
- **Idempotency:** Safe to retry without side effects
- **Correlation ID:** Request tracing across services

---

## Questions & Contact

**For roadmap feedback:** Open GitHub Discussion or email info@nac.best

**For feature requests:** File GitHub Issue with "enhancement" label.

**For security concerns:** Email info@nac.best (responsible disclosure).

