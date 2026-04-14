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

## nac.json Update

Add entity name to module's entities array:

```json
{
  "modules": {
    "{Module}": {
      "path": "src/Modules/{Namespace}.Modules.{Module}",
      "entities": ["{Entity}"],
      "features": []
    }
  }
}
```

## File Location

```
src/Modules/{Namespace}.Modules.{Module}/
└── Domain/
    └── Entities/
        └── {Entity}.cs
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
