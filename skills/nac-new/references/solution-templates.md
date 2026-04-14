# Solution Templates

Replace `{Name}` with solution name. Replace `{localNacPath}` if `--local-nac` provided. Replace `{NacVersion}` with resolved NAC framework version (see SKILL.md version resolution step).

## {Name}.slnx

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/{Name}.Host/{Name}.Host.csproj" />
  </Folder>
  <Folder Name="/src/Modules/" />
  <Folder Name="/tests/" />
</Solution>
```

## Directory.Build.props

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

## Directory.Packages.props

### PackageReference Mode (Default)

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- NAC Framework -->
    <PackageVersion Include="Nac.Abstractions" Version="{NacVersion}" />
    <PackageVersion Include="Nac.Domain" Version="{NacVersion}" />
    <PackageVersion Include="Nac.Mediator" Version="{NacVersion}" />
    <PackageVersion Include="Nac.WebApi" Version="{NacVersion}" />
    <PackageVersion Include="Nac.Observability" Version="{NacVersion}" />
    <PackageVersion Include="Nac.Persistence" Version="{NacVersion}" />
    <PackageVersion Include="Nac.Persistence.PostgreSQL" Version="{NacVersion}" />
    <!-- 3rd-party -->
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="7.2.0" />
  </ItemGroup>
</Project>
```

### ProjectReference Mode (--local-nac)

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- 3rd-party only (NAC packages use ProjectReference) -->
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="7.2.0" />
  </ItemGroup>
</Project>
```

## {Name}.Host.csproj

### PackageReference Mode (Default)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <PackageReference Include="Nac.Abstractions" />
    <PackageReference Include="Nac.Mediator" />
    <PackageReference Include="Nac.WebApi" />
    <PackageReference Include="Nac.Observability" />
    <PackageReference Include="Swashbuckle.AspNetCore" />
  </ItemGroup>

</Project>
```

### ProjectReference Mode (--local-nac)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <ProjectReference Include="{localNacPath}/src/Nac.Abstractions/Nac.Abstractions.csproj" />
    <ProjectReference Include="{localNacPath}/src/Nac.Mediator/Nac.Mediator.csproj" />
    <ProjectReference Include="{localNacPath}/src/Nac.WebApi/Nac.WebApi.csproj" />
    <ProjectReference Include="{localNacPath}/src/Nac.Observability/Nac.Observability.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" />
  </ItemGroup>

</Project>
```

## Program.cs

```csharp
using System.Reflection;
using Nac.Abstractions.Extensions;
using Nac.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "{Name} API", Version = "v1" });
});

builder.AddNacFramework(nac =>
{
    // Modules will be registered here by '/nac-add-module'
});

var app = builder.Build();

app.UseNacWebApi();
app.UseNacFramework();

// Swagger UI (Development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "{Name} API v1"));
}

// Welcome page
var startTime = DateTime.UtcNow;
const string welcomeHtml = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{NAME} API</title>
    <style>
        :root { --bg:#fafafa;--text:#1a1a1a;--accent:#6366f1;--card:#fff;--muted:#6b7280;--border:#e5e7eb; }
        @media(prefers-color-scheme:dark){:root{--bg:#0a0a0a;--text:#fafafa;--accent:#818cf8;--card:#171717;--muted:#9ca3af;--border:#374151;}}
        *{margin:0;padding:0;box-sizing:border-box;}
        body{font-family:system-ui,-apple-system,sans-serif;background:var(--bg);color:var(--text);min-height:100vh;display:flex;align-items:center;justify-content:center;}
        .c{text-align:center;padding:2rem;}
        .logo{width:80px;height:80px;margin:0 auto 1.5rem;}
        .logo svg{fill:var(--accent);}
        h1{font-size:1.75rem;font-weight:600;margin-bottom:.25rem;}
        .v{color:var(--muted);font-size:.875rem;margin-bottom:1rem;}
        .env{display:inline-block;padding:.25rem .75rem;border-radius:9999px;background:var(--accent);color:#fff;font-size:.75rem;font-weight:500;margin-bottom:1.5rem;}
        .links{display:flex;gap:.75rem;justify-content:center;flex-wrap:wrap;margin-bottom:2rem;}
        .links a{display:inline-flex;align-items:center;gap:.5rem;padding:.5rem 1rem;border-radius:.5rem;background:var(--card);border:1px solid var(--border);color:var(--text);text-decoration:none;font-size:.875rem;transition:border-color .15s;}
        .links a:hover{border-color:var(--accent);}
        .s{color:var(--muted);font-size:.75rem;line-height:1.75;}
    </style>
</head>
<body>
    <div class="c">
        <div class="logo"><svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg"><path d="M50 5L90 25v50L50 95 10 75V25L50 5zm0 10L20 30v40l30 15 30-15V30L50 15z"/><path d="M50 35l20 10v20L50 75 30 65V45l20-10zm0 8l-12 6v12l12 6 12-6V49l-12-6z"/></svg></div>
        <h1>{NAME} API</h1>
        <p class="v">v{VERSION}</p>
        <span class="env">{ENV}</span>
        <div class="links">
            <a href="/swagger" style="display:{SWAGGER_DISPLAY}">Swagger UI</a>
            <a href="/health">Health Check</a>
            <a href="/swagger/v1/swagger.json" style="display:{SWAGGER_DISPLAY}">OpenAPI Spec</a>
        </div>
        <div class="s">Uptime: {UPTIME}<br>Started: {STARTED} UTC</div>
    </div>
</body>
</html>
""";

app.MapGet("/", (IHostEnvironment env) =>
{
    var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    var uptime = DateTime.UtcNow - startTime;
    var html = welcomeHtml
        .Replace("{NAME}", "{Name}")
        .Replace("{VERSION}", version)
        .Replace("{ENV}", env.EnvironmentName)
        .Replace("{STARTED}", startTime.ToString("yyyy-MM-dd HH:mm:ss"))
        .Replace("{UPTIME}", $"{(int)uptime.TotalHours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}")
        .Replace("{SWAGGER_DISPLAY}", env.IsDevelopment() ? "inline-flex" : "none");
    return Results.Content(html, "text/html");
});

app.Run();
```

## Properties/launchSettings.json

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

## appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database={Name};Username=postgres;Password=postgres"
  }
}
```

## nac.json

### Default

```json
{
  "framework": { "name": "nac", "version": "{NacVersion}" },
  "solution": { "name": "{Name}", "namespace": "{Name}" },
  "database": {
    "provider": "postgresql",
    "connectionStringKey": "DefaultConnection"
  },
  "modules": {}
}
```

### With --local-nac

```json
{
  "framework": { "name": "nac", "version": "{NacVersion}" },
  "solution": { "name": "{Name}", "namespace": "{Name}" },
  "database": {
    "provider": "postgresql",
    "connectionStringKey": "DefaultConnection"
  },
  "modules": {},
  "localNacPath": "{localNacPath}"
}
```
