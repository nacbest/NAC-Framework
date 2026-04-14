# CLI Module Structure Fix (v1.0.1)

**Date**: 2026-04-13
**Severity**: Medium
**Component**: NAC CLI
**Status**: Resolved

## What Happened

The `nac add module` command was creating a flat folder structure without proper DDD layer separation. Modules contained bare Domain/ and Infrastructure/ folders instead of the expected 9 subfolders for a clean architecture implementation.

## The Brutal Truth

This was a lazy implementation from the start—we shipped a template that didn't match our own architectural guidelines. Any developer using this command got a broken structure they'd have to fix manually. The entity path was even worse, dumping files at the wrong level.

## Technical Details

- **Issue**: AddCommand.cs created only 2 placeholder folders; entity files landed in Domain/ instead of Domain/Entities/
- **Fix**: Created explicit subfolder generation for all 9 layers (Domain/Entities, Domain/Events, Domain/Specifications, Application/Commands, Application/Queries, Application/EventHandlers, Infrastructure/Persistence, Infrastructure/Repositories, Endpoints)
- **Namespace fix**: Entity generator now builds correct paths: `{Module}.Domain.Entities`

## What We Tried

Direct fix in AddCommand.cs with hardcoded folder creation—simple and immediate. No need for overengineering here.

## Root Cause

Template was designed as a skeleton, not a real scaffold. We prioritized speed over usability and shipped incomplete.

## Lessons Learned

CLI scaffolding tools must match architectural reality. If your docs say "9 layers," the generator must create them. Test the output, not just the code.

## Next Steps

- Monitor `nac add module` usage in real projects
- Add integration tests verifying complete folder structure
- Version bump to 1.0.1 (done)
