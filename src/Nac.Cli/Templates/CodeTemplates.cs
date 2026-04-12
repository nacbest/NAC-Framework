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

    public static string HostCsproj(string name) => """
        <Project Sdk="Microsoft.NET.Sdk.Web">

          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
          </PropertyGroup>

        </Project>
        """;

    public static string ProgramCs(string name) => $$"""
        var builder = WebApplication.CreateBuilder(args);

        // builder.AddNacFramework(nac =>
        // {
        //     nac.AddModule<...>();
        //     nac.UsePostgreSql();
        // });

        var app = builder.Build();

        // app.UseNacWebApi();
        // app.UseNacFramework();

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

    public static string ModuleCsproj(string ns, string moduleName) => $$"""
        <Project Sdk="Microsoft.NET.Sdk">

          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
            <RootNamespace>{{ns}}.Modules.{{moduleName}}</RootNamespace>
          </PropertyGroup>

        </Project>
        """;

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
        using Nac.Mediator.Core;
        using Nac.WebApi;

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

    public static string NacJson(string name, string dbProvider = "postgresql") => $$"""
        {
          "framework": { "name": "nac", "version": "1.0.0" },
          "solution": { "name": "{{name}}", "namespace": "{{name}}" },
          "database": {
            "provider": "{{dbProvider}}",
            "connectionStringKey": "DefaultConnection"
          },
          "modules": {}
        }
        """;
}
