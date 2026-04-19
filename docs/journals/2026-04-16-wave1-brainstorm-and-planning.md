# Wave 1 Planning: Locked CQRS, Persistence, Caching Architecture

**Date**: 2026-04-16 11:46
**Severity**: N/A (Planning)
**Component**: NAC Framework L1/L2 (Nac.Cqrs, Nac.Persistence, Nac.Caching)
**Status**: Complete — Phase files generated, implementation ready

## What Happened

Post-L0 brainstorm session to chart next iteration. Spawned 3 parallel researchers on custom CQRS patterns, EventBus abstractions, and multi-tenancy strategies. User reviewed outputs and decisively scoped Wave 1 to three packages. Created 7-phase implementation plan with clear dependencies and success criteria. Parallel research approach compressed what would've been sequential exploration into 30 minutes.

## The Decisions (Locked)

**CQRS Dispatcher**
- Custom FrozenDictionary implementation, no MediatR — 4.4x faster, ~60 LOC
- Sealed handlers enforced, ValueTask<T> for async-only scenarios
- 4 pipeline behaviors: Validation (FluentValidation) → Logging → Caching → Transaction
- Reflection-based handler scanning (source generators deferred to v2)

**Persistence Layer**
- IUnitOfWork interface added to Nac.Core — decouples Cqrs package from Persistence
- EF Core 10, consumer picks database provider (PostgreSQL, SQL Server, etc.)
- Polling-based outbox pattern embedded for future EventBus integration
- Transaction management via Behavior pipeline

**Caching Strategy**
- HybridCache (.NET 9+) wrapper in Nac.Caching
- ICacheableQuery marker interface in Nac.Cqrs
- Tenant-aware key generation (multitenancy prepped in Wave 2)
- Behavior-based automatic invalidation hooks

**Event Versioning**
- Design artifact created (how to version events, handle schema evolution)
- Implementation deferred to Wave 2 (EventBus release)

## Why This Matters

L0 shipped with clean domain/aggregate foundations. L1/L2 now adds query optimization + persistence mechanics without premature EventBus complexity. CQRS dispatcher being custom eliminates framework lock-in and gives performance transparency. IUnitOfWork abstraction in Core prevents circular package dependencies.

The scoped wave (just 3 packages) keeps scope realistic. EventBus, multitenancy, identity all valuable but blocked on Wave 1 foundations.

## What Almost Happened (Alternatives Rejected)

- **MediatR integration**: Fast bootstrapping, but adds reflection overhead + third-party lock-in
- **Event Sourcing from start**: Tempting, but increases Wave 1 scope by 40%. Deferred to Wave 3+
- **Finbuckle from Wave 1**: Multitenancy is complex enough to deserve dedicated attention. Wave 2 isolates it cleanly

## The Roadmap (Broader Context)

- **Wave 1** (current): CQRS + Persistence + Caching — 7 phases
- **Wave 2**: MultiTenancy (Finbuckle + PostgreSQL RLS) + EventBus (pluggable transport)
- **Wave 3**: Identity (ClaimsPrincipal integration) + Jobs (background workers)
- **Wave 4**: Testing framework + WebApi template + Observability (OpenTelemetry)
- **Wave 5**: Project templates + reference examples

## Research Artifacts Generated

Three parallel researcher reports consumed and synthesized:
- `plans/reports/researcher-260416-1145-custom-cqrs-dispatcher.md` — Performance justification, sealed handler patterns
- `plans/reports/researcher-260416-1128-eventbus-abstraction.md` — Transport pluggability, outbox mechanics
- `plans/reports/researcher-260416-1128-multi-tenancy-strategies.md` — Finbuckle RLS hybrid, scoping considerations

Implementation plan structure (`plans/260416-1146-wave1-cqrs-persistence-caching/`):
- 7 phase files with dependencies mapped
- Phased testing strategy (unit → integration)
- Success criteria for Go/No-Go at each gate

## Lessons

1. **Parallel research accelerates scope clarity** — three researchers in parallel beats sequential discussion
2. **Scoped waves prevent architecture thrashing** — "5 waves" reduces "everything at once" paralysis
3. **IUnitOfWork abstraction early prevents pain later** — learned from L0 that tight coupling between packages feels good until it doesn't
4. **Custom CQRS is worth 60 LOC** — transparency + performance wins over "framework standard"

## Next Steps

1. **Tester team**: Run L0 regression (190 tests should still pass)
2. **Implementation**: Start Phase 1 (project structure + IUnitOfWork interface in Nac.Core)
3. **Checkpoint**: After Phase 2, validate EF Core scaffolding works without errors
4. **No blockers**: All prerequisites from L0 in place

**Owner**: Implementation team  
**Timeline**: Phased over next 2 sprints (target: L1 release in 5 working days, L2 in 3 days after)
