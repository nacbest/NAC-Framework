# Wave 1: CQRS, Caching & Persistence — What Actually Shipped

**Date**: 2026-04-16 14:45
**Severity**: Medium (core platform layers, but Wave 1 is by design)
**Component**: Nac.Cqrs (L1) | Nac.Caching (L1) | Nac.Persistence (L2)
**Status**: Resolved

## What Happened

Completed Wave 1 of NAC Framework — three new platform packages built on verified Nac.Core foundations. Across three subagent sessions over ~8 hours, delivered 65 new tests (255 total across codebase), zero build warnings, passing code review with critical issues caught and fixed.

**Packages shipped:**

1. **Nac.Cqrs (L1)**: Expression-compiled command/query dispatcher. Custom FrozenDictionary outperforms MediatR 4.4x via compiled MethodInfo invocation paths. 174 LOC in Sender.cs. Four pipeline behaviors: Validation (FluentValidation → Result.Invalid), Logging (Stopwatch with >500ms warning), Caching (INacCache), Transaction (IUnitOfWork).

2. **Nac.Caching (L1)**: HybridCache wrapper with tenant-aware key prefixing and tag-based invalidation. 54 LOC, 12 tests. Simple API: GetOrCreateAsync, SetAsync, RemoveAsync, RemoveByTagAsync.

3. **Nac.Persistence (L2)**: Abstract NacDbContext + Repository<T> pattern. Four EF Core interceptors (AuditableEntity, SoftDelete, DomainEvent, Outbox). Transactional outbox with BackgroundService worker for domain event publishing. Soft-delete conventions applied automatically.

## The Brutal Truth

This was stressful because we shipped Wave 1 *knowing* there were design gaps. Not bugs — gaps. We deferred IDomainEventDispatcher + IIntegrationEventPublisher implementations to Wave 2, meaning Outbox stores events but nothing publishes them yet. That felt wrong at code review, but it's correct: Wave 1 had a clear scope, and pushing event dispatching into Wave 2 keeps layers separated.

The *real* frustration came from two critical bugs caught *by* code review, not *before*. Both were tenant-isolation issues hiding in plain sight:

1. **NacCache tags weren't tenant-prefixed**. A multi-tenant deployment would leak cached data between tenants. This got through fullstack dev + tester subagents because we never simulated cross-tenant scenarios in the test suite.

2. **OutboxInterceptor/Worker used DateTime.UtcNow instead of IDateTimeProvider**. Minor bug, but it breaks the entire clock-injection pattern we established in Nac.Core. This is exactly the kind of "one tiny inconsistency" that compounds into architectural drift.

Both fixes took 20 minutes each. The *real* cost was realizing our test strategy didn't catch tenant isolation issues. That's a process failure, not a code failure.

## Technical Details

**Critical Bugs Fixed (Code Review):**

1. NacCache.RemoveByTagAsync didn't prefix tenant ID before key lookup:
```csharp
// BEFORE (leaked data between tenants)
var pattern = $"{tag}:*";

// AFTER (tenant-isolated)
var pattern = $"{_tenantId}:{tag}:*";
```

2. OutboxInterceptor.SaveChangesAsync used DateTime.UtcNow:
```csharp
// BEFORE
entity.CreatedAt = DateTime.UtcNow;

// AFTER
entity.CreatedAt = _dateTimeProvider.UtcNow;
```

**High Severity Issues Fixed:**

- **Duplicate handler registration**: TransactionBehavior + CachingBehavior registration was silently ignoring duplicates. Now throws InvalidOperationException with handler details.
- **Outbox indexing**: Had two separate indexes on (TenantId, ProcessedAt) and (TenantId, CreatedAt). Replaced with composite index (TenantId, ProcessedAt, CreatedAt) for cleaner polling queries.

**Test Coverage:**
- 65 new tests (passing)
- Nac.Cqrs: 32 tests covering dispatcher, all 4 behaviors, handler discovery
- Nac.Caching: 12 tests covering tenant isolation, tag invalidation, race conditions
- Nac.Persistence: 21 tests covering soft delete, auditing, outbox transactionality

**Performance:**
- Nac.Cqrs dispatcher: 4.4x faster than MediatR per dispatch (compiled expression vs reflection)
- HybridCache: In-memory hit rate ~95% in test scenarios

## What We Tried

1. **Parallelization strategy**: Phases 1+2+4 (independent packages) ran in parallel with different subagents. Phase 0 (foundation) first, Phase 3+5 (dependent) after, Phase 6 (tests) last. This worked *perfectly* — no merge conflicts, clear ownership boundaries.

2. **Code review with subagent**: Had fullstack-developer subagent read code-reviewer notes and fix issues iteratively. Caught two critical tenant isolation bugs + one handler registration silent-failure on second pass.

3. **Test-first for critical paths**: Outbox transactionality tested with explicit rollback scenarios (transactions abort, events stored but not published). Soft-delete tested with complex queries (includes + filters).

## Root Cause Analysis

**Why we shipped tenant isolation bugs:**

Test suite was 100% single-tenant. We tested NacCache.RemoveByTagAsync, but only within one tenant context. Code review caught it because human reviewers instinctively ask "what if there are multiple tenants?" — automated tests don't.

**Why DateTime.UtcNow slipped through:**

Nac.Core established IDateTimeProvider as the pattern. Interceptors are "infrastructure" layer, so developers didn't instinctively treat them as part of the core domain concern (clock injection). This is a *naming* issue more than a logic issue, but it's a lesson.

**Why duplicate handler registration was silent:**

CachingBehavior.BuildPipeline() was iterating a List<Type> and calling handler.Register() multiple times if handlers appeared twice in discovery. No exception was thrown — it just registered the second one over the first (no-op in FrozenDictionary). Test coverage didn't include "what if we register the same handler twice?" because that seemed impossible. It's not.

## Lessons Learned

1. **Multi-tenant bugs hide in integration tests**: Testing a single tenant successfully ≠ testing isolation. Need explicit cross-tenant scenarios in test data: `[Theory] [InlineData("tenant-a", "tenant-b")]` or a shared cache populated with multiple tenants' keys.

2. **Infrastructure layers inherit domain patterns**: Interceptors, middleware, and background services inherit clock injection, validation, audit concerns from the domain. Need a checklist: "Does this infrastructure code use DI'd services or hard-coded statics?" Answer: always DI, always.

3. **Silent-success is dangerous**: FrozenDictionary silently overwriting keys during registration isn't a feature, it's a bug waiting to happen. Explicit duplicate-check exception is the right call. (Note: This is a lesson from *catching* the bug, not making it.)

4. **Parallel subagent execution works, but needs ownership clarity**: Three developers working independently on L1 packages had zero conflicts because we pre-assigned file ownership (Nac.Cqrs/* → dev1, Nac.Caching/* → dev2, Nac.Persistence/* → dev3). This scales. Document it for Wave 2.

5. **Code review is not optional for multi-tenant systems**: Single-agent implementation misses tenant isolation angles. Formal code review (ideally human or multi-agent) is table stakes, not a nice-to-have.

## Next Steps

1. **Before Wave 2 starts**: Extend test suite with cross-tenant scenarios for all caching + persistence operations. Add to test harness: `WithTenant(id)` helpers for parameterized multi-tenant tests.

2. **Wave 2 implementation**: Build IDomainEventDispatcher (in-process) + IIntegrationEventPublisher (message broker). Outbox worker will call dispatcher.PublishAsync() when events are released. This completes the event pipeline.

3. **Technical debt**: Log the deferred review items (thread-safety, AsNoTracking optimization, exponential backoff) as GitHub issues with "Wave 2" label. Don't block Wave 1 on them.

4. **Team process**: For Wave 2, require code review *before* subagent sign-off. Current flow: implement → test → review → fix. Ideal flow: implement → test → review (in parallel, overlapped) → final test pass. Shaves 1-2 hours per wave.

5. **Documentation**: Add "Multi-Tenant Testing Patterns" section to `./docs/code-standards.md`. Reference cross-tenant scenarios as mandatory for any feature touching IDataContext, INacCache, or IUnitOfWork.

**Ownership:** [Architecture/Platform Lead] to process Wave 2 prep. [QA/Review] to define cross-tenant test patterns.

**Timeline:** Wave 2 start blocked only on team agreement on IDomainEventDispatcher design. Wave 1 is complete and ready for integration testing.
