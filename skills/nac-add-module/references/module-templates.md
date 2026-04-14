# Module Templates

Replace `{Namespace}` from nac.json, `{Module}` from argument.

## {Namespace}.Modules.{Module}.csproj

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

## Solution Updates

### .slnx - Add under `/src/Modules/`

```xml
<Folder Name="/src/Modules/">
  <Project Path="src/Modules/{Namespace}.Modules.{Module}/{Namespace}.Modules.{Module}.csproj" />
</Folder>
```

### Host.csproj - Add ProjectReference

```xml
<ItemGroup>
  <ProjectReference Include="../Modules/{Namespace}.Modules.{Module}/{Namespace}.Modules.{Module}.csproj" />
</ItemGroup>
```

### Program.cs - Register Module

Find the `AddNacFramework` block and add:

```csharp
builder.AddNacFramework(nac =>
{
    nac.AddModule<{Namespace}.Modules.{Module}.{Module}Module>();
    // Other modules...
});
```

### nac.json - Add to modules

```json
{
  "modules": {
    "{Module}": {
      "path": "src/Modules/{Namespace}.Modules.{Module}",
      "entities": [],
      "features": []
    }
  }
}
```

## Directory Structure

Create these directories (can be empty initially):

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
├── Infrastructure/
│   ├── Persistence/
│   └── Repositories/
└── Endpoints/
```

Use `.gitkeep` files to preserve empty directories if needed.
