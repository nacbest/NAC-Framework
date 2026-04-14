---
name: nac-add-feature
description: "Add CQRS feature to NAC module"
argument-hint: "<Module>/<FeatureName>"
---

# Add CQRS Feature

Creates Command + Handler + Endpoint for a module.

## Prerequisites

- `nac.json` with module registered
- Module exists from `/nac-add-module`

## Arguments

| Arg | Required | Description |
|-----|----------|-------------|
| `<Module>/<FeatureName>` | Yes | e.g., `Catalog/CreateProduct` |

## Workflow

```mermaid
flowchart TD
    A[Parse Module/Feature] --> B{Valid?}
    B -->|No| C[Error: Use Module/Feature]
    B -->|Yes| D[Read nac.json]
    D --> E{Module exists?}
    E -->|No| F[Error: Run /nac-add-module]
    E -->|Yes| G{Feature exists?}
    G -->|Yes| H[Error: Feature exists]
    G -->|No| I[HARD-GATE: Confirm]
    I -->|Rejected| J[Abort]
    I -->|Approved| K[Generate Files]
    K --> L[Update Module]
    L --> M[Update nac.json]
    M --> N[dotnet build]
    N --> O{Build OK?}
    O -->|No| P[Fix & Retry]
    O -->|Yes| Q[Report Success]
```

## Steps

### 1. Parse Input
- Format: `Module/Feature` (e.g., `Catalog/CreateProduct`)
- Both must be PascalCase

### 2. Read nac.json
- Extract `namespace`
- Verify module in `modules`

### 3. Validate
- Module path exists
- Feature not exists (check Command file)

### 4. HARD-GATE: Confirm
```
AskUserQuestion: "Create feature '{Feature}' in '{Module}'?
- Application/Commands/{Feature}Command.cs
- Application/Commands/{Feature}Handler.cs
- Endpoints/{Feature}Endpoint.cs
- Update {Module}Module.cs
Proceed?"
```

### 5. Generate Files
- Load `references/cqrs-templates.md`
- Create Command, Handler, Endpoint

### 6. Update Module
- Add endpoint mapping in {Module}Module.cs
- Add feature to nac.json `features` array

### 7. Verify
```bash
dotnet build
```

## Error Recovery

| Error | Resolution |
|-------|------------|
| Module not found | Run `/nac-add-module` first |
| Feature exists | Choose different name |
| Build fails | Check imports, namespace |
