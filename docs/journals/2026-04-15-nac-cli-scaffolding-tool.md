# Nac.Cli: dotnet Global Tool for Project Scaffolding

**Date**: 2026-04-15 19:03
**Severity**: Low
**Component**: Nac.Cli
**Status**: Resolved

## What Happened

Built `Nac.Cli` — a `dotnet global tool` exposing a `nac new <ProjectName>` command that scaffolds a complete NAC Framework project tree (Host, Module core+infra, Shared, Tests) from 22 Scriban templates.

## The Brutal Truth

Two hours of the session were burned on an MSBuild trap that shouldn't exist: `.cs.sbn` files silently compiled as C# source instead of being embedded as resources. No error, no warning — the templates simply weren't there at runtime. That's the kind of failure that makes you distrust your own toolchain.

The Scriban version in the original plan (5.12.0) had known CVEs. Catching it during code review rather than before planning is embarrassing. A 30-second `dotnet list package --vulnerable` before writing the plan would have saved the churn.

## Technical Details

- **Silent MSBuild drop**: The .NET SDK auto-includes `**/*.cs` as `<Compile>` items. Files named `*.cs.sbn` matched that glob, so they were compiled (and failed silently as embedded resources). Fix: rename to `*.cstemplate` + explicit `<Compile Remove="Templates/**" />` in the csproj.
- **CVE upgrade**: Scriban 5.12.0 → 7.1.0.
- **Security gaps caught in review**: project/module name inputs had no sanitization — regex `^[A-Za-z][A-Za-z0-9]*$` added to block path traversal. Synchronous `File.WriteAllText` replaced with async. Child process stderr piped and surfaced. Exit codes added.
- Stack: `System.CommandLine 2.0.0-beta4`, `Scriban 7.1.0`, .NET 10.

## What We Tried

1. `.cs.sbn` as embedded resources — MSBuild swallowed them silently.
2. Renamed to `.cstemplate` + `<Compile Remove>` — worked.

## Root Cause Analysis

Two independent mistakes:
1. **MSBuild glob collision**: naming templates with `.cs.` in the extension is asking for trouble. The SDK's auto-include behavior is documented but easy to forget when you're focused on the template engine, not the build system.
2. **Dependency vetting skipped at planning time**: the researcher agent pulled a version number without running a vulnerability check. That should be a mandatory step before any dependency lands in a plan.

## Lessons Learned

- Never use `.cs.` anywhere in an embedded resource filename in a .NET SDK project.
- Run `dotnet list package --vulnerable` (or check NVD) before pinning any NuGet version in a plan.
- Input sanitization for CLI tools accepting names that become file paths is not optional — add it in the initial implementation, not in a follow-up review.

## Next Steps

- Add `dotnet list package --vulnerable` to the pre-plan checklist for any session adding NuGet dependencies. Owner: planning workflow. No deadline — just enforce it next time.
- Consider a csproj linting step that warns when `Templates/` content is being compiled rather than embedded.
