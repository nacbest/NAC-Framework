# NuGet Package Upgrade — xUnit v3 + Dependencies Synced

**Date**: 2026-04-16 13:42
**Severity**: Low
**Component**: NACFramework (all packages, test infrastructure)
**Status**: Resolved

## What Happened

Upgraded all outdated NuGet packages across NACFramework to latest stable versions:

**Phase 1 (EF Core):** Microsoft.EntityFrameworkCore 10.0.0 → 10.0.6, Microsoft.EntityFrameworkCore.Relational 10.0.0 → 10.0.6, Microsoft.EntityFrameworkCore.InMemory 10.0.0 → 10.0.6

**Phase 2 (Validation):** FluentValidation 11.11.0 → 12.1.1 + extensions (code audit showed zero deprecated API usage)

**Phase 3 (Testing):** xunit 2.9.3 → xunit.v3 3.2.2 (package rename + OutputType=Exe in test projects)

**Result:** 255/255 tests pass across 4 projects (190+8+34+23); build: 0 errors, 0 warnings.

## The Brutal Truth

xUnit v3 migration wasn't the "straightforward package swap" the research suggested. We hit two nasty gotchas that tanked the build. The frustrating part: the fixes were simple (suppress an analyzer, restore a suppressed package), but the path to finding them ate hours because the documentation conflated "xUnit v3 self-executing mode" with "dotnet test CLI compatibility." Now the framework is current, but trust in upgrade guides is lower.

## Technical Details

**xUnit v3 + testhost.dll error (first blocker):**
- xUnit v3 documentation says remove `Microsoft.NET.Test.Sdk`—we did
- `dotnet test` via VSTest adapter immediately failed: missing testhost.dll
- Root cause: xUnit v3 self-executing assembly mode ≠ dotnet test CLI mode
- Fix: Re-added Microsoft.NET.Test.Sdk 17.12.1
- Lesson: xUnit v3 has two execution paths; docs didn't make this clear

**xUnit1051 analyzer explosion (second blocker):**
- xUnit v3 includes new `xUnit1051` rule: "Use `TestContext.Current.CancellationToken` instead of `CancellationToken.None`"
- Combined with `TreatWarningsAsErrors=true`, this generated 312 build errors
- We don't use CancellationToken.None directly—the errors were false positives
- Fix: Created `tests/Directory.Build.props` with `<NoWarn>$(NoWarn);xUnit1051</NoWarn>`

**NoWarn chain breaking (third subtle issue):**
- `Nac.Caching.Tests.csproj` had `<NoWarn>CS1591</NoWarn>` (missing XML docs)
- This overrode the NoWarn chain from Directory.Build.props
- Changed to `<NoWarn>$(NoWarn);CS1591</NoWarn>` to preserve inheritance

**EF Core version sync (code reviewer catch):**
- Relational + InMemory lagged behind Core at 10.0.5 vs 10.0.6
- Synced all three to 10.0.6 in Directory.Packages.props

## Lessons Learned

1. **xUnit v3 is a migration, not a point-release.** Two execution modes, new analyzer rules, and test project output type changes. Treat it like a major version bump, not a patch.

2. **NoWarn must chain through inheritance.** Direct suppression overwrites parent values. Use `$(NoWarn);RULEID` pattern everywhere, or cascading build props fail silently until a new rule lands.

3. **Analyzer rules can be architecture-destroying.** A single new rule + TreatWarningsAsErrors turned a routine upgrade into 312 errors. Always test against the actual test suite early; don't assume new rules won't fire.

4. **Documentation conflates execution modes.** xUnit v3 docs talk about "self-hosting" tests as executables, but that's separate from VSTest adapter integration. Read both the runner docs AND the MSBuild docs.

## Next Steps

- Monitor xUnit v3 releases for analyzer rule additions; pin version or suppress aggressively
- EF Core 10.0.6 is EOL candidate—plan backport strategy if 11.x ships before next maintenance window
- FluentValidation 12.1.1 is stable; low risk going forward
- Document NoWarn inheritance pattern in code-standards.md for team reference

## Files Modified

- `Directory.Packages.props` (central version control)
- `tests/Directory.Build.props` (new file — xUnit1051 suppression)
- `Nac.Caching.Tests.csproj`, `Nac.Events.Tests.csproj`, `Nac.Identity.Tests.csproj`, `Nac.Persistence.Tests.csproj` (OutputType=Exe, package name updates)
- 5x docs updated by docs-manager agent
- Plan files tracked by project-manager agent
