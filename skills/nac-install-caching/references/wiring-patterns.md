# Caching Wiring Patterns

## Program.cs — In-memory (default)

```csharp
using Nac.Caching.Extensions;

// In service configuration
builder.Services.AddNacCaching();
```

## Program.cs — Redis

```csharp
using Nac.Caching.Extensions;

// Redis cache provider (must be before AddNacCaching)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "{Namespace}:";
});
builder.Services.AddNacCaching();
```

## appsettings.json — Redis

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

## Host.csproj — PackageReference Mode (Default)

```xml
<!-- Always required -->
<PackageReference Include="Nac.Caching" />

<!-- Redis provider (add only if Redis chosen) -->
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" />
```

## Host.csproj — ProjectReference Mode (localNacPath in nac.json)

```xml
<!-- Always required -->
<ProjectReference Include="../../src/Nac.Caching/Nac.Caching.csproj" />

<!-- Redis provider (add only if Redis chosen) -->
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" />
```

## Directory.Packages.props — Add entries for new packages

```xml
<!-- Add if not already present -->
<PackageVersion Include="Nac.Caching" Version="{NacVersion}" />
<!-- Redis only -->
<PackageVersion Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.0" />
```

## Usage Examples

```csharp
// Cached query — implement ICacheable
public record GetProductsQuery : IQuery<List<ProductDto>>, ICacheable
{
    public string CacheKey => "products:all";
    public TimeSpan? Expiry => TimeSpan.FromMinutes(10);
}

// Cache invalidation on command — implement ICacheInvalidator
public record CreateProductCommand : ICommand<Guid>, ICacheInvalidator
{
    public required string Name { get; init; }
    public IEnumerable<string> CacheKeysToInvalidate =>
        ["products:all"];
}
```
