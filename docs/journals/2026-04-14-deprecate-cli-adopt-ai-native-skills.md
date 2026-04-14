# Deprecate NAC CLI in Favor of AI-Native Skills

**Date**: 2026-04-14 12:30
**Severity**: High
**Component**: Nac.Cli, NAC Framework Architecture
**Status**: COMPLETED

## What Happened

Made a strategic decision to deprecate `Nac.Cli` entirely and replace it with 5 AI-native skills bundled in `NAC/skills/`. The CLI was designed for manual developer use, but NAC's entire philosophy is AI-friendly development. We were fighting against that premise by maintaining a tool that requires compilation, installation, and human interaction.

## The Brutal Truth

This is honestly overdue. We've been pretending the CLI is central to NAC when really it's a distraction—it requires developers to compile .NET code just to scaffold a feature, and it's yet another tool to maintain, version, and troubleshoot. The real strength of NAC is that AI agents understand modular architecture and CQRS patterns natively. We should lean into that instead of forcing everything through a CLI bottleneck.

## Decision Details

**Deprecation Plan Created**: `/Users/nhan/Documents/code/NAC/plans/260414-1155-nac-skills-strategy/`

**5 AI-Native Skills** (replaces 5 CLI commands):
1. `nac-new` — Generate solution scaffold
2. `nac-add-module` — Add module with folder structure
3. `nac-add-entity` — Add domain entity with migrations
4. `nac-add-feature` — Add CQRS command/query + handler + endpoint
5. `nac-install-identity` — Install Nac.Identity package + wire DI

**Guardrails**:
- HARD-GATE confirmation on all skill outputs (no auto-apply)
- Mandatory `dotnet build` after every scaffold (syntax check)
- `nac.json` as single source of truth for project context
- Target audience: Developers already familiar with NAC (concise docs only)

## Technical Details

**Scope**: 8 hours of work across 6 phases:
- Phase 1: Create `nac.json` schema and validation
- Phase 2: Implement 5 skills (scaffold, struct, domain, cqrs, identity)
- Phase 3: Self-healing via dotnet build integration
- Phase 4: Document skill outputs and confirmation flow
- Phase 5: Deprecate Nac.Cli from codebase
- Phase 6: Testing and validation

**Architecture**:
```
NAC/skills/
├── nac-new/
├── nac-add-module/
├── nac-add-entity/
├── nac-add-feature/
└── nac-install-identity/
```

Each skill outputs to stdout, requires HARD-GATE approval before file creation, then runs `dotnet build` to validate syntax.

## Why This Matters

NAC is fundamentally an AI-friendly framework. Pushing everything through a manually-compiled CLI contradicts that. Skills:
- Are AI-native (no compilation barrier)
- Can self-improve via dotnet build feedback loops
- Allow contextual decisions (AI reads `nac.json` for tenant strategy, entity patterns, etc.)
- Don't require versioning/distribution overhead

This is what the framework should have been from the start.

## Lessons Learned

1. **Don't build tools for humans when your framework targets AI**: If the entire value prop is "AI understands your architecture," then the developer experience should be AI-first, not human-first.

2. **Maintenance tax is real**: CLI requires compilation, versioning, documentation, and debugging. Skills require documentation only—AI agents self-heal via output validation.

3. **Context-aware scaffolding matters**: A skill can read `nac.json`, understand tenant strategy, and generate code that fits. A CLI prompt is static.

## Implementation Results

**Completed**: 2026-04-14 12:30 (same day as decision)

**Deliverables**:
- `skills/nac-new/` — SKILL.md + 2 reference files (solution-templates, project-docs)
- `skills/nac-add-module/` — SKILL.md + 1 reference file (module-templates)
- `skills/nac-add-entity/` — SKILL.md + 1 reference file (entity-templates)
- `skills/nac-add-feature/` — SKILL.md + 1 reference file (cqrs-templates)
- `skills/nac-install-identity/` — SKILL.md + 3 reference files (auth-endpoints, migration-safety, tenant-flows)

**Cleanup**:
- Deleted `src/Nac.Cli/` (8 files)
- Updated `Nac.slnx` (removed Nac.Cli project)
- Updated `CLAUDE.md` (skills commands instead of CLI)

**Verification**:
- `dotnet build Nac.slnx` — passes with 0 errors
- Code review — minor concerns addressed (placeholder documentation)

**Commit**: `74fb98b` — `refactor: deprecate CLI, adopt AI-native skills`

The cleanup is done. NAC now feels coherent with its AI-first philosophy.
