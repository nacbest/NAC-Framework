# Wave 2 Architecture Decisions: All 6 L2 Packages Locked

**Date**: 2026-04-16 14:00
**Severity**: Medium
**Component**: NAC Framework Wave 2 (EventBus, MultiTenancy, Identity, Jobs, Testing, Observability)
**Status**: Resolved (Plan Created)

## What Happened

Completed brainstorming and architecture review for all 6 remaining L2 packages. After 4 rounds of Q&A with user and 2 parallel researcher agents investigating EventBus transport options and MultiTenancy patterns, locked all tech decisions and created detailed Wave 2A implementation plan.

## Technical Decisions Made

1. **Nac.EventBus**: Custom thin abstraction + InMemory Channel-based transport. Rejected Wolverine (bus factor=1, framework coupling) and MassTransit (v9 going commercial, v8 EOL Dec 2026). YAGNI principle: NAC only needs pub/sub, no sagas.

2. **Nac.MultiTenancy**: Custom EF Core 10 global query filters (HasQueryFilter). Rejected Finbuckle as overkill—NAC already has ITenantEntity. Leverages existing ICurrentUser context.

3. **Nac.Identity**: Direct ASP.NET Core Identity wrapper. Implement IPermissionChecker, ICurrentUser from ClaimsPrincipal. Zero abstraction debt.

4. **Nac.Jobs**: Abstractions only (IJobScheduler, IRecurringJob) + SimpleJobScheduler using BackgroundService. Rejected Hangfire (LGPL copyleft) and Quartz.NET (unnecessary complexity).

5. **Nac.Testing**: In-memory fakes only. Every Nac.Core abstraction gets a matching fake. No Testcontainers (YAGNI for libraries).

6. **Nac.Observability**: ILogger + OpenTelemetry only. Zero Serilog dependency—consumers bring their own logging provider.

## Key Insight

EventBus architecture bridges Persistence's OutboxWorker (untyped: string eventType + JSON payload) to typed IEventHandler<TEvent> dispatch via OutboxEventPublisher. Deserializes and routes through InMemory Channel. Clean separation of concerns.

## Delivery Strategy: Sub-Waves

- **2A**: EventBus + Testing (first—event flow foundation)
- **2B**: Identity + MultiTenancy (second—tenant-scoped auth)
- **2C**: Observability + Jobs (third—low dependency)

## Result

Minimal external dependencies. Almost all packages depend only on Microsoft abstractions. Consistent YAGNI/KISS approach from Wave 1. Detailed plan created in `plans/260416-1345-wave2a-eventbus-testing/` with 7 phases, 14h effort estimate, 100+ test target.
