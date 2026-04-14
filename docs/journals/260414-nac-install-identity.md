# Journal: nac install identity

**Date:** 2026-04-14  
**Status:** Completed

## Summary

Verified và hoàn thành implementation của `nac install identity` CLI command. Command đã được implement trước đó, session này focus vào verification.

## What Was Done

### Phase 01: IdentityTemplates.cs
- 6 template methods: ProgramCsUsings, AddNacIdentityServices, UseNacIdentityMiddleware, AppSettingsNacIdentitySection, CsprojPackageReference, CsprojProjectReference

### Phase 02: InstallCommand.cs
- CreateIdentityCommand() registered trong Create()
- InstallIdentityAsync() với full flow: ReadManifest → validate Host → add references → update Program.cs → update appsettings.json → verify build
- Idempotency check (skip if AddNacIdentity already in Program.cs)
- Support both localNacPath (ProjectReference) và NuGet (PackageReference)

### Phase 03: Verification
- Fresh install: Pass
- Idempotency: Pass (shows "already installed")
- Build verification: Pass
- File structure: Correct

## Test Results

| Test | Result |
|------|--------|
| Fresh project install | Pass |
| Run twice (idempotency) | Pass |
| Host.csproj references | Correct |
| Program.cs injection | Correct |
| appsettings.json merge | Correct |
| Build after install | Pass |
| Missing nac.json | Exception (could be friendlier) |
| Missing Host | Friendly error |

## Code Review Notes

Minor issues noted for future:
- Process.Start() null check missing
- ReadManifest throws raw exceptions
- No file locking (low risk for CLI)

## Docs Updated

- CLAUDE.md: Added `nac install identity` và `nac install skill` to CLI Commands section

## Next Steps for Users

1. Update appsettings.json SigningKey
2. Run EF migrations: `dotnet ef migrations add InitialIdentity -p src/Nac.Identity -s src/{Ns}.Host`
3. Install Claude skill: `nac install skill identity`
