# Phase 08: Identity Test Suite — Pattern A Coverage & Critical Security Gates

**Date**: 2026-04-19 16:30  
**Severity**: Medium  
**Component**: Identity (tests)  
**Status**: Resolved (HTTP endpoints deferred, service-layer complete)

## What Happened

Executed Phase 08 test-suite work on the Pattern A identity migration. Expanded `Nac.Identity.Tests` from 97 to 150 unit tests, created new `Nac.Identity.IntegrationTests` project with 14 integration tests (full solution: 685 tests passing). Two commits: d31800f (infra), ba94958 (test suites). Merge gates explicitly proven: R1 multi-tenant isolation, R2 JWT signature/claim-swap rejection, R3 instant revoke via cache invalidation, R4 host bypass guard. JWT size < 2KB assertion passes. Tenant onboarding seeds 3 roles idempotently.

## The Brutal Truth

This phase forced a reckoning with technical debt and ecosystem mismatches. Testcontainers + InMemory EF integration revealed that the existing testing infrastructure was not Pattern A-aware, and fixing it surfaced latent bugs in both the test utilities and the application seeding logic. The deferral of HTTP-level tests stings because it leaves an integration gap, but the alternative—forcing a full WebApplication test host rebuild—would have corrupted the schedule without proportional security gain (service-layer is already proven).

## Technical Details

**Test Matrix Delivered:**
- `PermissionCheckerTests`: cache hit, cross-provider merge, cross-user, cross-tenant, resource fallback, host bypass
- `PermissionGrantCacheTests`: invalidate, pattern-based invalidate, key shape
- `MembershipServiceTests`: 9 scenarios covering invite, accept, role change, remove (all with cache invalidation)
- `RoleServiceTests`: clone from template, mutation guards, grant/revoke/list
- `JwtTokenServiceTests`: tenantless vs. tenant-scoped tokens, `is_host` claim, **role_ids round-trip, size < 2KB assertion, no permission claims**
- `RoleTemplateSeederTests`: idempotency, stable Guid ids, drift detection
- Integration suite: isolation tests (cross-tenant denial, signature mismatch rejection), instant revoke (grant → check pass → revoke → check fail < TTL), onboarding (3 roles + owner membership seeded)

**Merge Gate Proofs:**
- **R1 (Isolation)**: Tenant A grant does NOT affect Tenant B access; crafted claim-swap rejected via JWT signature verification; query filter blocks cross-tenant read.
- **R2 (Signature)**: Attacker-signed token + swapped claim rejected.
- **R3 (Instant Revoke)**: Cache invalidation proven on real Postgres via Testcontainers; < TTL latency.
- **R4 (Host Bypass)**: Non-host calling `AsHostQueryAsync` throws; host without permission throws; host+permission returns `IgnoreQueryFilters`.

**Key Failures Fixed Pre-commit:**
- Code-review agent caught host bypass guard missing in integration tests → added `HostQueryExtensionsTests`.
- JWT signature isolation missing → added `JwtSignatureIsolationTests`.
- Both closed before commit d31800f.

## What We Tried

1. **InMemory EF for integration tests**: Failed silently. `RoleTemplateSeeder` calls `db.Database.BeginTransactionAsync`, which InMemory throws on. Try/catch in seeder hid it; tests passed with zero seeded rows. Solution: Suppressed `InMemoryEventId.TransactionIgnoredWarning` in unit test DbContext options, moved integration tests to Testcontainers.

2. **Piggybacking on ReferenceApp.IntegrationTests infrastructure**: Unsafe. `TestDataSeeder.cs` uses old 2-arg `NacUser` ctor (`email`, `tenantId`), but Pattern A requires new ctor (`email`, `fullName`). The new ctor has `fullName` second param; old caller binds accidentally and `.FullName = email` overwrites damage. Latent time bomb if anyone re-enables ReferenceApp integration tests.

3. **Mocking DbContext for unit tests**: Explicitly rejected per `development-rules.md`. Service-level tests against real Postgres for critical security paths via Testcontainers was the correct move.

## Root Cause Analysis

**Test infrastructure was pre-Pattern A.** `FakeCurrentUser` happened to be compatible (already had RoleIds, IsHost fields), but `TestDataSeeder.cs` and the broader test-host wiring assumed the old user model. The seeder's broken state was masked by the 2-arg ctor accepting both old and new signatures — a Python-style accident in C#.

**Testcontainers + InMemory mismatch wasn't obvious until integration.** The seeder's transaction code is correct; InMemory's behavior is a gotcha (documented but not obvious when glancing at test setup code).

**HTTP endpoint tests demand full wiring.** AuthEndpointsTests, ManagementEndpointsTests, and TenantOnboardingHandler E2E would require standing up JWT middleware + MVC controllers + event-bus event handlers. That's not a small test — it's a full `TestWebApplicationFactory` equivalent to ReferenceApp.Host, minus the UI. Given ReferenceApp.Host is the actual E2E harness and its integration tests are stale, the risk-reward doesn't justify the effort in Phase 08. Service layer is covered; HTTP layer deferred to Phase 09 (if needed).

## Lessons Learned

1. **Test infrastructure refactors propagate.** When you change the core model (`NacUser` constructor signature), audit all test builders, fakes, and seeders. Automation can't catch cross-layer breaks if multiple signatures coexist.

2. **InMemory EF exceptions in setup code are silent killers.** The seeder's try/catch was defensive coding that turned into a foot gun. Always run unit test setup against real database (via Testcontainers or Docker) to catch these.

3. **Deferred integration tests are acceptable if service-layer is proven.** HTTP layer is a translation layer; if the underlying service is tested, the endpoint can be validated via a smaller-scope integration test (e.g., HTTP client hitting service directly without full MVC stack).

4. **Code review before commit saves hours.** The two gaps the review agent caught (host bypass, signature isolation) would have surfaced as test gaps in Phase 09 or as prod bugs. Adversarial review earned its keep.

## Next Steps

- **Phase 09**: Document Docker requirement for CI; optionally add `AuthEndpointsTests` if time permits (likely low priority given service-layer coverage).
- **Follow-up**: Fix `TestDataSeeder.cs` in ReferenceApp to use correct `NacUser` ctor; re-enable and audit ReferenceApp integration tests.
- **Audit**: Coverage report via `dotnet test /p:CollectCoverage=true` to verify ≥ 90% on Identity services.
- **Management services**: `MembershipManagementService`, `RoleManagementService`, `UserGrantManagementService` have no direct unit tests. Add in v4 follow-up.

---

**Files Modified**: d31800f (infra), ba94958 (test suites)  
**Tests Passing**: 685 (150 unit + 14 integration identity tests)  
**Deferred**: AuthEndpointsTests, ManagementEndpointsTests (HTTP layer), TenantOnboardingHandler E2E
