# Nac.Core L0 Foundation Package — Phase Complete

**Date**: 2026-04-16 00:00
**Severity**: Low
**Component**: Nac.Core (L0 DDD Foundation)
**Status**: Resolved

## What Happened

Built Nac.Core, the zero-dependency DDD foundation for NAC Framework: 40 source files (~530 LOC), 190 passing unit tests, zero compiler warnings (TreatWarningsAsErrors enabled). Delivered 7 phases from solution infrastructure through value objects.

## The Brutal Truth

This was the correct foundational work. Every decision here cascades to L1+ packages. The code is small, well-tested, and handles the invisible plumbing that will prevent architectural chaos later. No heroics—just solid blocking and tackling.

## Technical Details

**Architecture Decisions:**
- ApplicationInitializationContext uses `IServiceProvider` (not `IApplicationBuilder`) to keep Core free of ASP.NET Core deps
- ICurrentUser.Id hardcoded as Guid—conscious simplification for v0.1
- Concrete events (UserRegisteredEvent) stay in Core for now; can migrate to Nac.Identity.Contracts later

**Critical Bugs Found & Fixed (during code-reviewer phase):**
- Entity equality: Two transient entities (default Id) incorrectly equal → added guard checking for default ID
- Specification perf: Expression compiled per IsSatisfiedBy call → cached compiled delegates
- Specification EF compat: Expression.Invoke unsupported by EF Core → built ParameterReplacer visitor
- Result<T>.Value: Accessible on failures returning default → now throws InvalidOperationException
- MultiplePermissionGrantResult: Mutable Dictionary exposed → changed to IReadOnlyDictionary
- DependsOnAttribute: Array not defensively copied → fixed
- Address: Missing validation → added Guard calls for required fields

## Lessons Learned

1. **Expression compilation costs**: Lazy compilation caching prevents subtle perf degradation. This pattern must be tested early.
2. **EF Core expression limitations**: Expression.Invoke, LINQ.Async idioms won't work. Test Specifications against actual EF DbContext, not just LINQ-to-Objects.
3. **Defensive value object design**: Setters public for EF Core interceptor compat—document this friction point loudly. Future maintainers will ask "why?"
4. **Type safety vs simplicity trade-off**: Hardcoding ICurrentUser.Id as Guid is pragmatic now but constrains future multi-tenant identity schemes. Acceptable for v0.1; revisit at v1.0.

## Next Steps

- Nac.Core is ready for L1 consumption (Nac.Identity, Nac.Authorization, Nac.Events)
- No blocking dependencies; can proceed immediately with higher-level packages
- Monitor: Expression performance in real workloads; Entity equality with composite IDs if multi-tenant support required later
