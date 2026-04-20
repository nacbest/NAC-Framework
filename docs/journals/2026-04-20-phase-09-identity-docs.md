# Phase 09: Identity Docs Shipped — Two Rounds of Factual Corrections Required

**Date**: 2026-04-20 06:53–08:10
**Severity**: Medium
**Component**: Documentation (Identity v3.0)
**Status**: Resolved

## What Happened

Executed Phase 09 of Pattern A identity migration under auto-cook mode. Generated 566-line `docs/identity-and-rbac.md` with 12 sections including a Customer Identity Pattern Guide recipe (~150 LoC). Updated system architecture, codebase summary, project changelog (v3.0.0 BREAKING), and roadmap. Committed 8 files, +931/-128 lines.

Code reviewer caught two factual errors in the first pass (9.0/10) that would break consumer copy-paste usage — not style issues, but literal lies.

## The Brutal Truth

This is frustrating because the docs felt "complete" after writing, but the reviewer didn't rubber-stamp it. The errors weren't theoretical — they were active landmines: someone following the docs would write non-functional code. Two rounds of fixes before hitting 9.6/10 approval threshold. Wasted ~20 minutes on rework that a single `grep` pass earlier would have caught.

## Technical Details

**Error 1: `IJwtTokenService` phantom interface**
- Docs (in 3 places) prescribed using `IJwtTokenService` for token operations
- Reality: `JwtTokenService` is a sealed class, no interface exists
- Impact: Consumer code calling `.GetService<IJwtTokenService>()` fails with container resolution error
- Fix: Explicit "no interface in v3" note; added v4 roadmap candidate

**Error 2: `IPasswordHasher<Customer>` without registration context**
- Customer Identity Pattern Guide told readers to inject `IPasswordHasher<Customer>` directly
- Reality: `AddNacIdentity<T>` only wires `IPasswordHasher<NacUser>` by default
- Impact: Type mismatch unless consumer manually registers `IPasswordHasher<Customer>` first
- Fix: Added explicit DI snippet + checklist item: "Register your own hasher if using custom password type"

**Error 3 (caught on second pass): Same `IJwtTokenService` reference in `llms-full.txt:113`**

## What We Tried

1. Grepped for interface references during writing — only checked `Identity` service layer files
2. Assumed standard service patterns (`IFoo` interface → `Foo` implementation) held everywhere
3. Didn't cross-check docs against actual sealed classes until reviewer flagged it

## Root Cause Analysis

**Overconfidence in pattern familiarity.** The codebase uses interfaces for 90% of injectable services. I assumed `IJwtTokenService` existed because every other core service had one. It doesn't — that particular service is sealed intentionally (no extension point needed, token generation logic is framework-locked). This wasn't documented in the architecture docs I read beforehand.

**Single-file source verification.** Grepped only `ServiceCollectionExtensions.cs` for interface signatures, didn't cross-check against actual implementation files. A second grep for `sealed class JwtTokenService` would have caught this immediately.

**Pattern recipes are high-stakes docs.** They're not architecture explanation — they're executable instructions. A mistake here doesn't just confuse readers; it breaks their code. Should have treated the Customer Identity Pattern Guide with the same rigor as unit tests: write, test-compile in head, then verify.

## Lessons Learned

1. **For service/DI docs: always grep the implementation files, not just the registration layer.** `ServiceCollectionExtensions` shows what's wired but not the actual signature (interface vs. sealed class).
2. **When writing copy-paste recipes, simulate the consumer workflow.** Would a new developer following this get compiling code? Run through the DI container resolution path mentally.
3. **Scout-first workflow reduces review cycles.** The Explore agent read entity files upfront — I didn't have to interrupt writing to check `NacUser` shape or role templates. Parallel finalization (project-manager + docs-manager) was efficient but can't replace deep source reading.

## Next Steps

- ✅ Committed Phase 09 (commit `0a396de`)
- ✅ Marked Phase 08–09 as `done` in roadmap
- ✅ Marked "Pattern A Identity Migration" initiative as complete
- Deferred: v4 roadmap items (`IJwtTokenService` interface, custom password hasher overload) — noted in docs, not urgent
- Monitor: If v3.0 consumers report DI container errors on identity services, this session's decisions will be first place to check

**Emotional reality:** Relief more than frustration. Two rounds feels like a lot, but both catches were legitimately critical — shipping either error would have torched credibility. The fix was 15 minutes of work, not days of customer support.
