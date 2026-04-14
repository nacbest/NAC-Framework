# Module Templates

Replace `{Namespace}` from nac.json, `{Module}` from argument.

## Module Core: {Namespace}.Modules.{Module}.csproj

### PackageReference Mode (Default)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>{Namespace}.Modules.{Module}</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Nac.Abstractions" Version="1.0.0" />
    <PackageReference Include="Nac.Domain" Version="1.0.0" />
    <PackageReference Include="Nac.Mediator" Version="1.0.0" />
  </ItemGroup>

</Project>
```

### ProjectReference Mode (localNacPath in nac.json)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>{Namespace}.Modules.{Module}</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="{localNacPath}/src/Nac.Abstractions/Nac.Abstractions.csproj" />
    <ProjectReference Include="{localNacPath}/src/Nac.Domain/Nac.Domain.csproj" />
    <ProjectReference Include="{localNacPath}/src/Nac.Mediator/Nac.Mediator.csproj" />
  </ItemGroup>

</Project>
```

## Module Infrastructure: {Namespace}.Modules.{Module}.Infrastructure.csproj

### PackageReference Mode (Default)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>{Namespace}.Modules.{Module}.Infrastructure</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Nac.Persistence" Version="1.0.0" />
    <PackageReference Include="Nac.Persistence.PostgreSQL" Version="1.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../{Namespace}.Modules.{Module}/{Namespace}.Modules.{Module}.csproj" />
  </ItemGroup>

</Project>
```

### ProjectReference Mode (localNacPath in nac.json)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>{Namespace}.Modules.{Module}.Infrastructure</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="{localNacPath}/src/Nac.Persistence/Nac.Persistence.csproj" />
    <ProjectReference Include="{localNacPath}/src/Nac.Persistence.PostgreSQL/Nac.Persistence.PostgreSQL.csproj" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../{Namespace}.Modules.{Module}/{Namespace}.Modules.{Module}.csproj" />
  </ItemGroup>

</Project>
```

## {Module}Module.cs

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nac.Abstractions.Modularity;

namespace {Namespace}.Modules.{Module};

public sealed class {Module}Module : INacModule
{
    public string Name => "{Module}";
    public IReadOnlyList<Type> Dependencies => [];

    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        // Register module services here
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/{module-lowercase}");
        // Map module endpoints here
    }
}
```

Note: Replace `{module-lowercase}` with lowercase module name (e.g., "catalog", "orders").

## {Module}DbContext.cs

```csharp
using Microsoft.EntityFrameworkCore;
using Nac.Abstractions.Auth;
using Nac.Persistence;

namespace {Namespace}.Modules.{Module}.Infrastructure;

public sealed class {Module}DbContext : NacDbContext
{
    public {Module}DbContext(
        DbContextOptions<{Module}DbContext> options,
        ICurrentUser? currentUser = null)
        : base(options, currentUser) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof({Module}DbContext).Assembly);
    }
}
```

## {Module}InfrastructureExtensions.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nac.Persistence.Extensions;
using Nac.Persistence.PostgreSQL.Extensions;

namespace {Namespace}.Modules.{Module}.Infrastructure;

public static class {Module}InfrastructureExtensions
{
    public static IServiceCollection Add{Module}Infrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddNacPostgreSQL<{Module}DbContext>(connectionString);
        // {Module}Module lives in core project — accessible because .Infrastructure references core
        services.AddNacRepositoriesFromAssembly<{Module}DbContext>(
            typeof({Module}Module).Assembly);
        return services;
    }
}
```

## Solution Updates

### .slnx - Add under `/src/Modules/`

```xml
<Folder Name="/src/Modules/">
  <Project Path="src/Modules/{Namespace}.Modules.{Module}/{Namespace}.Modules.{Module}.csproj" />
  <Project Path="src/Modules/{Namespace}.Modules.{Module}.Infrastructure/{Namespace}.Modules.{Module}.Infrastructure.csproj" />
</Folder>
```

### Host.csproj - Add ProjectReferences (both projects)

```xml
<ItemGroup>
  <ProjectReference Include="../Modules/{Namespace}.Modules.{Module}/{Namespace}.Modules.{Module}.csproj" />
  <ProjectReference Include="../Modules/{Namespace}.Modules.{Module}.Infrastructure/{Namespace}.Modules.{Module}.Infrastructure.csproj" />
</ItemGroup>
```

### Program.cs - Register Module + Infrastructure

Find the `AddNacFramework` block and add:

```csharp
builder.AddNacFramework(nac =>
{
    nac.AddModule<{Namespace}.Modules.{Module}.{Module}Module>();
    // Other modules...
});
```

Add infrastructure wiring:

```csharp
builder.Services.Add{Module}Infrastructure(connectionString);
```

### nac.json - Add to modules

```json
{
  "modules": {
    "{Module}": {
      "path": "src/Modules/{Namespace}.Modules.{Module}",
      "infrastructurePath": "src/Modules/{Namespace}.Modules.{Module}.Infrastructure",
      "entities": [],
      "features": []
    }
  }
}
```

## Directory Structure

Create these directories (can be empty initially):

**Module core:**
```
src/Modules/{Namespace}.Modules.{Module}/
├── Domain/
│   ├── Entities/
│   ├── Events/
│   └── Specifications/
├── Application/
│   ├── Commands/
│   ├── Queries/
│   └── EventHandlers/
├── Contracts/
└── Endpoints/
```

**Module infrastructure:**
```
src/Modules/{Namespace}.Modules.{Module}.Infrastructure/
├── Configurations/
└── Repositories/
```

Use `.gitkeep` files to preserve empty directories if needed.
