# Module Infrastructure Split: 2-Project-Per-Module Architecture

**Date**: 2026-04-14 15:52
**Severity**: Medium
**Component**: Module Architecture / Scaffolding Skills
**Status**: Resolved

## What Happened

Adopted Option B (2-project-per-module split) across documentation and scaffolding skills. Every business module now generates two projects:
- `{Ns}.Modules.{M}` — Domain, Application, Contracts, Endpoints. Zero EF Core refs.
- `{Ns}.Modules.{M}.Infrastructure` — DbContext, Configurations, Repositories, DI extension.

This was a docs + skills change only (commit `9471bdd`). No runtime code touched.

## The Brutal Truth

We had a half-baked architecture where module persistence responsibilities were split inconsistently — some modules dumped EF configs into core, others left repositories in a grey zone. The old `CLAUDE.md` said "Host creates the DbContext" but gave no clear home for configurations or custom repository implementations. Scaffolding skills generated a single project that leaked EF Core into module core. This was going to hurt the moment a second developer joined.

## Technical Details

Files changed across 5 locations:
- `CLAUDE.md` — dependency diagram updated, new Module Architecture section, forbidden pattern list extended
- `skills/nac-add-module/SKILL.md` + `module-templates.md` — now emits 2 `.csproj` files
- `skills/nac-add-entity/SKILL.md` + `entity-templates.md` — `IEntityTypeConfiguration<T>` generated in `.Infrastructure`, not core
- `docs/system-architecture.md` — Persistence Architecture section added
- `docs/code-standards.md` — folder structure, repository pattern, DI registration conventions

Two bugs caught in code review and fixed before merge:
- **H1**: `.Infrastructure.csproj` template was missing `Nac.Persistence.PostgreSQL` package reference — it would have compiled without it only by accident via transitive deps, breaking isolation
- **H2**: `{Module}Module` bootstrap type in core needs to be `public` for `.Infrastructure` DI extension to reference it — added clarifying comment since the template already had it correct but the intent wasn't documented

`nac.json` schema extended with `infrastructurePath` field so skills know where to emit infrastructure files.

## Root Cause Analysis

The original single-project layout was a shortcut that worked fine for a solo prototype. The `CLAUDE.md` hinted at a cleaner separation ("module never references Nac.Persistence") but the scaffolding skills didn't enforce it. Undocumented conventions are not conventions — they're time bombs.

## Lessons Learned

- Write the architecture rule AND update the scaffolding tool simultaneously. A rule without enforcement is a suggestion.
- Code-review the templates, not just the prose docs. H1 (missing package ref) would have silently worked via transitive resolution, making the bug invisible until someone cleaned up deps.
- `nac.json` schema changes need a migration note — existing solutions using the old schema will silently ignore `infrastructurePath` and get wrong output from skills.

## Next Steps

- [ ] Add `infrastructurePath` migration note to `skills/nac-add-module/SKILL.md` for existing projects — **owner: any dev picking up a module task**
- [ ] Validate generated output by running `nac-add-module` end-to-end against a throwaway solution — not done yet, skills were only reviewed as templates
- [ ] Update `docs/codebase-summary.md` to reflect 2-project module layout — currently still describes single-project modules
