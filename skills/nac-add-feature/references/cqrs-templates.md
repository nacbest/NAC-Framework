# CQRS Templates

Replace `{Namespace}` from nac.json, `{Module}` and `{Feature}` from argument.

**Derived placeholders:**
- `{module-lowercase}` = lowercase of `{Module}` (e.g., Catalog → catalog)
- `{feature-lowercase}` = lowercase of `{Feature}` (e.g., CreateProduct → createproduct)

## Command Pattern (Write Operations)

### {Feature}Command.cs (Basic)

```csharp
using Nac.Core.Messaging;

namespace {Namespace}.Modules.{Module}.Application.Commands;

public sealed record {Feature}Command() : ICommand<Guid>;
```

### {Feature}Command.cs (With Markers)

```csharp
using Nac.Core.Messaging;

namespace {Namespace}.Modules.{Module}.Application.Commands;

public sealed record {Feature}Command(
    string Name,
    decimal Price
) : ICommand<Guid>, ITransactional, IRequirePermission
{
    public string Permission => "{module-lowercase}.{feature-lowercase}";
}
```

### {Feature}Handler.cs (Basic)

```csharp
using Nac.Core.Messaging;
using Nac.Mediator.Abstractions;

namespace {Namespace}.Modules.{Module}.Application.Commands;

public sealed class {Feature}Handler : ICommandHandler<{Feature}Command, Guid>
{
    public Task<Guid> HandleAsync({Feature}Command command, CancellationToken ct)
    {
        // TODO: Implement {Feature} logic
        throw new NotImplementedException();
    }
}
```

### {Feature}Handler.cs (With DI)

```csharp
using Nac.Core.Messaging;
using Nac.Mediator.Abstractions;

namespace {Namespace}.Modules.{Module}.Application.Commands;

public sealed class {Feature}Handler(
    I{Entity}Repository repository
) : ICommandHandler<{Feature}Command, Guid>
{
    public async Task<Guid> HandleAsync({Feature}Command command, CancellationToken ct)
    {
        var entity = {Entity}.Create(command.Name, command.Price);
        repository.Add(entity);
        return entity.Id;
        // UnitOfWork behavior calls SaveChanges automatically
    }
}
```

## {Feature}Endpoint.cs

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nac.WebApi;
using Nac.Mediator.Core;

namespace {Namespace}.Modules.{Module}.Endpoints;

public static class {Feature}Endpoint
{
    public static void Map{Feature}(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/{feature-lowercase}", async (
            {Namespace}.Modules.{Module}.Application.Commands.{Feature}Command command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.SendAsync(command, ct);
            return Results.Ok(ApiResponse<Guid>.Success(result));
        });
    }
}
```

Note: Replace `{feature-lowercase}` with lowercase feature name.

## Query Pattern (Read Operations)

### {Feature}Query.cs

```csharp
using Nac.Core.Messaging;

namespace {Namespace}.Modules.{Module}.Application.Queries;

public sealed record {Feature}Query(Guid Id) : IQuery<{Entity}Dto?>;
```

### {Feature}Query.cs (With Caching)

```csharp
using Nac.Core.Messaging;

namespace {Namespace}.Modules.{Module}.Application.Queries;

public sealed record {Feature}Query(Guid Id) : IQuery<{Entity}Dto?>, ICacheable
{
    public string CacheKey => $"{module-lowercase}:{Id}";
    public TimeSpan? Expiry => TimeSpan.FromMinutes(5);
}
```

### {Feature}QueryHandler.cs

```csharp
using Nac.Core.Messaging;
using Nac.Mediator.Abstractions;

namespace {Namespace}.Modules.{Module}.Application.Queries;

public sealed class {Feature}QueryHandler : IQueryHandler<{Feature}Query, {Entity}Dto?>
{
    public Task<{Entity}Dto?> HandleAsync({Feature}Query query, CancellationToken ct)
    {
        // TODO: Implement query logic
        throw new NotImplementedException();
    }
}
```

## Module Updates

### {Module}Module.cs - Add Endpoint Mapping

```csharp
public void ConfigureEndpoints(IEndpointRouteBuilder routes)
{
    var group = routes.MapGroup("/api/{module-lowercase}");
    group.Map{Feature}();  // Add this line
}
```

### nac.json - Add Feature

```json
{
  "modules": {
    "{Module}": {
      "path": "src/Modules/{Namespace}.Modules.{Module}",
      "entities": [],
      "features": ["{Feature}"]
    }
  }
}
```

## File Locations

```
src/Modules/{Namespace}.Modules.{Module}/
├── Application/
│   ├── Commands/
│   │   ├── {Feature}Command.cs
│   │   └── {Feature}Handler.cs
│   └── Queries/           # For query features
│       ├── {Feature}Query.cs
│       └── {Feature}QueryHandler.cs
└── Endpoints/
    └── {Feature}Endpoint.cs
```

## Marker Interfaces

| Interface | Purpose |
|-----------|---------|
| `ITransactional` | Wrap in DB transaction |
| `IRequirePermission` | Check `Permission` property |
| `ICacheable` | Cache query result |
| `ICacheInvalidator` | Invalidate cache keys |
| `IAuditable` | Log audit trail |
