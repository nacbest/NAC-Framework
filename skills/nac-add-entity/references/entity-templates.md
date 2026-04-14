# Entity Templates

Replace `{Namespace}` from nac.json, `{Module}` and `{Entity}` from argument.

## AggregateRoot (Default)

Use for root entities that own other entities and raise domain events.

```csharp
using Nac.Domain;

namespace {Namespace}.Modules.{Module}.Domain.Entities;

public sealed class {Entity} : AggregateRoot<Guid>
{
    // Required for EF
    private {Entity}() { }

    // Factory method pattern - add parameters as needed
    // Example: public static {Entity} Create(string name, decimal price)
    public static {Entity} Create()
    {
        var entity = new {Entity}
        {
            Id = Guid.NewGuid()
            // TODO: Initialize properties from factory parameters
        };
        
        // entity.RaiseDomainEvent(new {Entity}CreatedDomainEvent(entity.Id));
        return entity;
    }

    // TODO: Add properties (e.g., public string Name { get; private set; })
}
```

## Child Entity

Use for entities owned by an aggregate root.

```csharp
using Nac.Domain;

namespace {Namespace}.Modules.{Module}.Domain.Entities;

public sealed class {Entity} : Entity<Guid>
{
    // Required for EF
    private {Entity}() { }

    // TODO: Add properties
}
```

## Value Object

Use for immutable value types (Money, Address, etc.).

```csharp
using Nac.Domain;

namespace {Namespace}.Modules.{Module}.Domain.Entities;

public sealed class {Entity} : ValueObject
{
    public {Entity}(/* parameters */)
    {
        // Initialize properties
    }

    // TODO: Add properties

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        // yield return each property
        yield break;
    }
}
```

## {Entity}Configuration.cs (in .Infrastructure)

Generated in the module's `.Infrastructure` project under `Configurations/`.

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {Namespace}.Modules.{Module}.Domain.Entities;

namespace {Namespace}.Modules.{Module}.Infrastructure.Configurations;

public sealed class {Entity}Configuration : IEntityTypeConfiguration<{Entity}>
{
    public void Configure(EntityTypeBuilder<{Entity}> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}
```

## nac.json Update

Add entity name to module's entities array:

```json
{
  "modules": {
    "{Module}": {
      "path": "src/Modules/{Namespace}.Modules.{Module}",
      "infrastructurePath": "src/Modules/{Namespace}.Modules.{Module}.Infrastructure",
      "entities": ["{Entity}"],
      "features": []
    }
  }
}
```

## File Locations

**Entity** (module core):
```
src/Modules/{Namespace}.Modules.{Module}/
└── Domain/
    └── Entities/
        └── {Entity}.cs
```

**Configuration** (module infrastructure):
```
src/Modules/{Namespace}.Modules.{Module}.Infrastructure/
└── Configurations/
    └── {Entity}Configuration.cs
```

## Guidelines

1. **AggregateRoot** - entities that:
   - Have a unique identity (Id)
   - Own child entities
   - Enforce invariants across children
   - Raise domain events

2. **Entity** - entities that:
   - Have identity but belong to an aggregate
   - Cannot exist without their aggregate root

3. **ValueObject** - types that:
   - Have no identity
   - Are immutable
   - Equality based on properties

4. **Configuration** - always generated alongside entity:
   - Lives in `.Infrastructure` project (has EF Core dependency)
   - Entity file has NO EF Core references
   - Configuration references entity from core project
