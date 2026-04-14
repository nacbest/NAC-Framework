# Observability Wiring Patterns

## Program.cs

```csharp
using Nac.Observability.Extensions;

// Register early — outermost behavior captures full pipeline duration
builder.Services.AddNacObservability();
```

## Host.csproj

```xml
<ProjectReference Include="..\..\src\Nac.Observability\Nac.Observability.csproj" />
```

## What Gets Logged

```
[INF] Handling command CreateProductCommand...
[INF] Command CreateProductCommand completed in 45ms
[ERR] Command CreateProductCommand failed after 12ms: NullReferenceException: ...

[INF] Handling query GetProductsQuery...
[INF] Query GetProductsQuery completed in 8ms
```

## Registration Order Guidance

```csharp
// Recommended order in Program.cs:
builder.Services.AddNacObservability();    // 1. Outermost — logs everything
builder.Services.AddNacAuthorization();    // 2. Auth check
builder.Services.AddNacCaching();          // 3. Cache hit/miss
// ... other services
```
