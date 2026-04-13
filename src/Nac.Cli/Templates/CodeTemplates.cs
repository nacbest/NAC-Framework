namespace Nac.Cli.Templates;

/// <summary>
/// Embedded code templates for scaffolding. Uses raw string literals with
/// double-dollar interpolation to handle C# braces in output.
/// </summary>
internal static class CodeTemplates
{
    public static string SlnxFile(string name) => $$"""
        <Solution>
          <Folder Name="/src/">
            <Project Path="src/{{name}}.Host/{{name}}.Host.csproj" />
          </Folder>
          <Folder Name="/src/Modules/" />
          <Folder Name="/tests/" />
        </Solution>
        """;

    public static string HostCsproj(string name, string? localNacPath = null)
    {
        // Host references implementation packages for infrastructure setup
        var refs = localNacPath != null
            ? $"""
                  <ItemGroup>
                    <ProjectReference Include="{localNacPath}/src/Nac.Abstractions/Nac.Abstractions.csproj" />
                    <ProjectReference Include="{localNacPath}/src/Nac.Mediator/Nac.Mediator.csproj" />
                    <ProjectReference Include="{localNacPath}/src/Nac.WebApi/Nac.WebApi.csproj" />
                    <ProjectReference Include="{localNacPath}/src/Nac.Observability/Nac.Observability.csproj" />
                  </ItemGroup>
              """
            : """
                  <ItemGroup>
                    <PackageReference Include="Nac.Abstractions" Version="1.0.0" />
                    <PackageReference Include="Nac.Mediator" Version="1.0.0" />
                    <PackageReference Include="Nac.WebApi" Version="1.0.0" />
                    <PackageReference Include="Nac.Observability" Version="1.0.0" />
                  </ItemGroup>
              """;

        return $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">

              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>

            {refs}
            </Project>
            """;
    }

    public static string ProgramCs(string name) => $$"""
        using Nac.Abstractions.Extensions;
        using Nac.WebApi.Extensions;

        var builder = WebApplication.CreateBuilder(args);

        builder.AddNacFramework(nac =>
        {
            // Modules will be registered here by 'nac add module'
        });

        var app = builder.Build();

        app.UseNacWebApi();
        app.UseNacFramework();

        app.MapGet("/", () => "Hello from {{name}}!");

        app.Run();
        """;

    public static string AppSettings(string name) => $$"""
        {
          "Logging": {
            "LogLevel": {
              "Default": "Information",
              "Microsoft.AspNetCore": "Warning"
            }
          },
          "ConnectionStrings": {
            "DefaultConnection": "Host=localhost;Database={{name}};Username=postgres;Password=postgres"
          }
        }
        """;

    public static string ModuleCsproj(string ns, string moduleName, string? localNacPath = null)
    {
        // Modules only reference abstractions - no implementation packages
        var refs = localNacPath != null
            ? $"""
                  <ItemGroup>
                    <ProjectReference Include="{localNacPath}/src/Nac.Abstractions/Nac.Abstractions.csproj" />
                    <ProjectReference Include="{localNacPath}/src/Nac.Domain/Nac.Domain.csproj" />
                    <ProjectReference Include="{localNacPath}/src/Nac.Mediator/Nac.Mediator.csproj" />
                  </ItemGroup>
              """
            : """
                  <ItemGroup>
                    <PackageReference Include="Nac.Abstractions" Version="1.0.0" />
                    <PackageReference Include="Nac.Domain" Version="1.0.0" />
                    <PackageReference Include="Nac.Mediator" Version="1.0.0" />
                  </ItemGroup>
              """;

        return $$"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <RootNamespace>{{ns}}.Modules.{{moduleName}}</RootNamespace>
              </PropertyGroup>

              <ItemGroup>
                <FrameworkReference Include="Microsoft.AspNetCore.App" />
              </ItemGroup>

            {{refs}}
            </Project>
            """;
    }

    public static string ModuleClass(string ns, string moduleName) => $$"""
        using Microsoft.AspNetCore.Builder;
        using Microsoft.AspNetCore.Routing;
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using Nac.Abstractions.Modularity;

        namespace {{ns}}.Modules.{{moduleName}};

        public sealed class {{moduleName}}Module : INacModule
        {
            public string Name => "{{moduleName}}";
            public IReadOnlyList<Type> Dependencies => [];

            public void ConfigureServices(IServiceCollection services, IConfiguration config)
            {
                // Register module services here
            }

            public void ConfigureEndpoints(IEndpointRouteBuilder routes)
            {
                var group = routes.MapGroup("/api/{{moduleName.ToLowerInvariant()}}");
                // Map module endpoints here
            }
        }
        """;

    public static string CommandFile(string ns, string module, string feature) => $$"""
        using Nac.Abstractions.Messaging;

        namespace {{ns}}.Modules.{{module}}.Application.Commands;

        public sealed record {{feature}}Command() : ICommand<Guid>;
        """;

    public static string HandlerFile(string ns, string module, string feature) => $$"""
        using Nac.Abstractions.Messaging;
        using Nac.Mediator.Abstractions;

        namespace {{ns}}.Modules.{{module}}.Application.Commands;

        public sealed class {{feature}}Handler : ICommandHandler<{{feature}}Command, Guid>
        {
            public Task<Guid> HandleAsync({{feature}}Command command, CancellationToken ct)
            {
                // TODO: Implement {{feature}} logic
                throw new NotImplementedException();
            }
        }
        """;

    public static string EndpointFile(string ns, string module, string feature) => $$"""
        using Microsoft.AspNetCore.Builder;
        using Microsoft.AspNetCore.Http;
        using Microsoft.AspNetCore.Routing;
        using Nac.Abstractions.WebApi;
        using Nac.Mediator.Core;

        namespace {{ns}}.Modules.{{module}}.Endpoints;

        public static class {{feature}}Endpoint
        {
            public static void Map{{feature}}(this IEndpointRouteBuilder routes)
            {
                routes.MapPost("/{{feature.ToLowerInvariant()}}", async (
                    {{ns}}.Modules.{{module}}.Application.Commands.{{feature}}Command command,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.SendAsync(command, ct);
                    return Results.Ok(ApiResponse<Guid>.Success(result));
                });
            }
        }
        """;

    public static string EntityFile(string ns, string module, string entity) => $$"""
        using Nac.Domain;

        namespace {{ns}}.Modules.{{module}}.Domain;

        public sealed class {{entity}} : AggregateRoot<Guid>
        {
            // TODO: Add properties and domain logic
        }
        """;

    public static string NacJson(string name, string dbProvider = "postgresql", string? localNacPath = null)
    {
        var localLine = localNacPath != null ? $",\n  \"localNacPath\": \"{localNacPath}\"" : "";
        return $$"""
            {
              "framework": { "name": "nac", "version": "1.0.0" },
              "solution": { "name": "{{name}}", "namespace": "{{name}}" },
              "database": {
                "provider": "{{dbProvider}}",
                "connectionStringKey": "DefaultConnection"
              },
              "modules": {}{{localLine}}
            }
            """;
    }

    public static string ClaudeMd(string name) => $$"""
        # {{name}} — NAC Framework Project

        ## Framework

        Built on **NAC Framework v1.0** — modular .NET 10 foundation with CQRS, multi-tenancy, and clean architecture.

        ## Quick Reference

        ### Code Generation
        ```bash
        nac add module <Name>              # New module
        nac add feature <Module>/<Name>    # Command + Handler + Endpoint
        nac add entity <Module>/<Name>     # Entity + Repository
        nac migration add <Module> "Desc"  # EF migration
        ```

        ### CQRS Pattern
        - **Commands** (write): `ICommand<T>` with marker interfaces
        - **Queries** (read): `IQuery<T>` with optional caching
        - **Handlers never call SaveChanges** — UnitOfWork behavior handles it

        ### Marker Interfaces
        - `ITransactional` — wrap in DB transaction
        - `IRequirePermission` — check `Permission` property
        - `ICacheable` — cache query result
        - `ICacheInvalidator` — invalidate cache keys post-command
        - `IAuditable` — log audit trail

        ## Code Patterns

        ### Command with Behaviors
        ```csharp
        public sealed record Create{{name}}Command(string Name)
            : ICommand<Guid>,
              ITransactional,
              IRequirePermission
        {
            public string Permission => "{{name.ToLowerInvariant()}}.create";
        }
        ```

        ### Cacheable Query
        ```csharp
        public sealed record Get{{name}}Query(Guid Id)
            : IQuery<{{name}}Dto>,
              ICacheable
        {
            public string CacheKey => $"{{name.ToLowerInvariant()}}:{Id}";
            public TimeSpan? Expiry => TimeSpan.FromMinutes(5);
        }
        ```

        ### Entity with Domain Event
        ```csharp
        public sealed class {{name}} : AggregateRoot<Guid>
        {
            public static {{name}} Create(string name)
            {
                var entity = new {{name}} { Id = Guid.NewGuid(), Name = name };
                entity.RaiseDomainEvent(new {{name}}CreatedDomainEvent(entity.Id));
                return entity;
            }
        }
        ```

        ## Conventions

        - **Commands:** `{Name}Command.cs` + `{Name}CommandHandler.cs`
        - **Queries:** `{Name}Query.cs` + `{Name}QueryHandler.cs`
        - **Permissions:** `module.resource.action` (e.g., `catalog.products.create`)
        - **Cache keys:** `entity:id` or `entity:list`

        ## Module Structure
        ```
        Modules/{Module}/
        ├── Domain/Entities/, Events/, Specifications/
        ├── Application/Commands/, Queries/, EventHandlers/
        ├── Infrastructure/Persistence/, Repositories/
        ├── Endpoints/
        └── {Module}Module.cs
        ```

        ## Testing
        Use Fakes from `Nac.Testing`: `FakeEventBus`, `FakeTenantContext`, `FakeCurrentUser`

        ## Documentation
        - [NAC Code Standards](https://github.com/nac-framework/docs/code-standards.md)
        - [NAC System Architecture](https://github.com/nac-framework/docs/system-architecture.md)
        """;

    public static string LlmsTxt(string name) => $$"""
        # {{name}}

        > .NET 10 backend API built on NAC Framework v1.0 with CQRS, modular architecture, and clean domain-driven design.

        ## Project Structure

        - CLAUDE.md: AI assistant instructions and code patterns
        - nac.json: Framework configuration and module registry
        - src/{{name}}.Host/: Composition root, Program.cs, DI setup
        - src/Modules/: Feature modules (Domain, Application, Infrastructure, Endpoints)
        - tests/: Unit and integration tests

        ## CLI Commands

        ```bash
        nac add module <Name>              # New module
        nac add feature <Module>/<Name>    # Command + Handler + Endpoint
        nac add entity <Module>/<Name>     # Entity + Repository
        nac migration add <Module> "Desc"  # EF migration
        nac migration apply                # Apply migrations
        nac check architecture             # Verify module boundaries
        ```

        ## CQRS Pattern

        ### Commands (Write Operations)

        ```csharp
        // Command with marker interfaces for behaviors
        public sealed record CreateProductCommand(string Name, decimal Price)
            : ICommand<Guid>,
              ITransactional,        // Wrap in DB transaction
              IRequirePermission,    // Check authorization
              IAuditable             // Log audit trail
        {
            public string Permission => "catalog.products.create";
        }

        // Handler - NEVER call SaveChanges (UnitOfWork handles it)
        public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
        {
            public async Task<Guid> Handle(CreateProductCommand cmd, CancellationToken ct)
            {
                var product = Product.Create(cmd.Name, cmd.Price);
                _repository.Add(product);
                return product.Id;  // UnitOfWork commits after handler
            }
        }
        ```

        ### Queries (Read Operations)

        ```csharp
        // Cacheable query
        public sealed record GetProductByIdQuery(Guid Id)
            : IQuery<ProductDto>,
              ICacheable
        {
            public string CacheKey => $"product:{Id}";
            public TimeSpan? Expiry => TimeSpan.FromMinutes(5);
        }
        ```

        ## Marker Interfaces

        | Interface | Purpose |
        |-----------|---------|
        | ITransactional | Wrap handler in DB transaction |
        | IRequirePermission | Check Permission property before handler |
        | ICacheable | Cache query result (CacheKey, Expiry) |
        | ICacheInvalidator | Invalidate cache keys after command |
        | IAuditable | Log audit trail |

        ## Domain Events

        ```csharp
        // Event raised from aggregate
        public sealed record ProductCreatedDomainEvent(Guid ProductId) : DomainEvent;

        // Entity raises event via factory method
        public sealed class Product : AggregateRoot<Guid>
        {
            public static Product Create(string name, decimal price)
            {
                var product = new Product { Id = Guid.NewGuid(), Name = name, Price = price };
                product.RaiseDomainEvent(new ProductCreatedDomainEvent(product.Id));
                return product;
            }
        }

        // Handler dispatched after transaction commits
        public sealed class ProductCreatedHandler : INotificationHandler<ProductCreatedDomainEvent>
        {
            public async Task Handle(ProductCreatedDomainEvent evt, CancellationToken ct)
            {
                await _eventBus.PublishAsync(new ProductCreatedIntegrationEvent(evt.ProductId), ct);
            }
        }
        ```

        ## Module Structure

        ```
        Modules/{Module}/
        ├── Domain/
        │   ├── Entities/{Entity}.cs
        │   ├── Events/{Event}DomainEvent.cs
        │   └── Specifications/{Spec}Spec.cs
        ├── Application/
        │   ├── Commands/{Command}Command.cs, {Command}CommandHandler.cs
        │   ├── Queries/{Query}Query.cs, {Query}QueryHandler.cs
        │   └── EventHandlers/
        ├── Infrastructure/
        │   ├── Persistence/{Module}DbContext.cs
        │   └── Repositories/
        ├── Endpoints/{Feature}Endpoints.cs
        └── {Module}Module.cs
        ```

        ## Module Registration

        ```csharp
        public sealed class CatalogModule : INacModule
        {
            public string Name => "Catalog";
            public IReadOnlyList<Type> Dependencies => [];

            public void ConfigureServices(IServiceCollection services, IConfiguration config)
            {
                services.AddNacPersistence<CatalogDbContext>(config);
                services.AddNacMediator(x => x.AddHandlers(typeof(CatalogModule).Assembly));
            }

            public void ConfigureEndpoints(IEndpointRouteBuilder routes)
            {
                var group = routes.MapGroup("/api/catalog");
                ProductEndpoints.MapProductEndpoints(group);
            }
        }
        ```

        ## Repository Pattern

        ```csharp
        // Specification encapsulates query - NO IQueryable exposure
        public sealed class GetProductsByPriceSpec : Specification<Product>
        {
            public GetProductsByPriceSpec(decimal min, decimal max)
            {
                Query.Where(p => p.Price >= min && p.Price <= max)
                     .OrderBy(p => p.Price)
                     .Take(100);
            }
        }

        // Usage
        var products = await _repository.GetAsync(new GetProductsByPriceSpec(10, 100), ct);
        ```

        ## Permission Format

        ```
        module.resource.action

        Examples:
        - catalog.products.create
        - orders.* (wildcard: all order permissions)
        - *.approve (wildcard: approve in any module)
        ```

        ## Pipeline Order

        Command: ExceptionHandling → Logging → Validation → Authorization → TenantEnrichment → UnitOfWork → Handler → SaveChanges → DomainEvents

        Query: ExceptionHandling → Logging → Validation → Authorization → CacheCheck → Handler → CacheStore

        ## Naming Conventions

        - Commands: {Name}Command.cs + {Name}CommandHandler.cs
        - Queries: {Name}Query.cs + {Name}QueryHandler.cs
        - Entities: PascalCase, inherit AggregateRoot<Guid> or Entity<Guid>
        - Events: {Name}DomainEvent.cs or {Name}IntegrationEvent.cs
        - Specs: {Name}Spec.cs
        - Constants: UPPER_SNAKE_CASE
        - Private fields: _camelCase

        ## Testing

        ```csharp
        // Use Fakes from Nac.Testing, not Moq
        var fakeEventBus = new FakeEventBus();
        var fakeUser = new FakeCurrentUser("user-id", ["orders.create"]);
        var fakeTenant = new FakeTenantContext(tenantId);

        // Verify events
        var published = fakeEventBus.PublishedOf<OrderCreatedIntegrationEvent>();
        Assert.NotEmpty(published);
        ```

        ## Exception Mapping

        | Exception | HTTP |
        |-----------|------|
        | ValidationException | 400 |
        | UnauthorizedException | 401 |
        | ForbiddenException | 403 |
        | NotFoundException | 404 |
        | ConflictException | 409 |
        | DomainException | 422 |
        """;
}
