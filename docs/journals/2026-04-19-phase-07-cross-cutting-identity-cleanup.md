# Phase 07: Cross-Cutting Cleanup + Host User - Completed

**Date**: 2026-04-19 21:30
**Severity**: Medium
**Component**: NacIdentity, MultiTenancy, WebApi
**Status**: Resolved

## What Happened

Phase 07 "Cross-cutting Cleanup + Host User" successfully closed 52 cascading compile errors from API contract changes in earlier phases. The scope was grep-sweep stale `NacUser.TenantId` usages, wire `IsHost` flag through JWT claims, introduce host-bypass query extension, and update samples. All 587 tests pass, zero build errors.

## The Brutal Truth

This phase was painful but necessary triage. Previous phases (01-06) changed four critical APIs — NacUser (removed TenantId), JwtTokenService (new pure generation), PermissionChecker (added cache + repo), ICurrentUser (added Name, RoleIds, IsHost) — but the test fakes and fixtures weren't updated in sync. The compiler noise was overwhelming: 52 errors scattered across 11 test files made it hard to see the pattern. The frustrating part was realizing mid-phase that **every breaking API change needs synchronized fake/builder updates**. We'll never catch that retroactively; it must be enforced during implementation.

## Technical Details

### Errors Fixed
- `NacUserTests`: CurrentUser now reads claims, not properties; refactored 8 test assertions
- `JwtTokenServiceTests`: Pure function signature changed; rewrote token validation checks
- `PermissionCheckerTests`: Added IPermissionRepository dependency; updated mocks
- `CurrentUserAccessorTests`: IsHost + Name extraction from claims (new properties)
- `FakeCurrentUser`: Synced property setters with claim-based reads
- `FakePermissionChecker`: Added InMemoryPermissionRepository for cache testing

### Key Architectural Decisions
1. **Defense in Depth for Host Access**: `IsHost` flag + `Host.AccessAllTenants` permission both required. `HostAdminOnlyFilter` checks both; `AsHostQueryAsync` awaits permission check before `IgnoreQueryFilters()`.
2. **ForbiddenAccessException → HTTP 403**: Code review caught that `UnauthorizedAccessException` was mapping to 401 (wrong semantic for authorization failure). Created dedicated exception type with correct status mapping.
3. **Rate Limiting on /auth/login**: ASP.NET rate limiter (5/min/IP, 429 response) prevents credential stuffing without test complexity.
4. **Opt-In Host Bypass**: `IgnoreQueryFilters` never automatic. Must call `AsHostQueryAsync<T>(...)` explicitly; throws `ForbiddenAccessException` if checks fail.

## Root Cause Analysis

The root issue: **API contracts changed; implementation updated; tests ignored.** We treated tests as validation checkboxes instead of executable specs. When Phase 02 introduced `ICurrentUser.Name` and Phase 05 added `IPermissionChecker.IsGrantedAsync()`, the test doubles weren't updated immediately. This created 52 errors by Phase 07. The lesson: breaking changes to abstractions must trigger immediate test double updates, not defer them.

## Lessons Learned

1. **Synchronize Fakes Atomically**: When you change an interface signature, update the fake in the same commit. Test failures are early warning signals, not cleanup debt.
2. **Claim-Based Identity**: Reading `IsHost` from JWT claims instead of querying the user object decouples tenantless operations from tenant context. Cleaner, but every layer must understand claims-first identity.
3. **Host Bypass Requires Ceremony**: Defense in depth (flag + permission) is verbose but prevents accidental admin-level access. The `AsHostQueryAsync` helper enforces this ceremony; it's worth the boilerplate.
4. **Compiler Stops You Here**: 52 errors at build time is unpleasant but precise. Better than 52 runtime NPEs in production.

## Next Steps

- Phase 08 (Unit & Integration Tests) expands host scenarios: seed host user, verify 403 on regular user accessing admin endpoints, test rate limiter blocks 6th request.
- Code review pattern: Cross-check ICurrentUser claim keys against NacIdentityClaims constants (already done for IsHost).
- Grep post-build: `rg 'NacUser.*TenantId'` confirms only docs/migration history remain (already clean).
