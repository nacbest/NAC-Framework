using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nac.Modularity;

/// <summary>
/// Represents a self-contained module in the NAC framework.
/// Each module owns its own domain, application logic, infrastructure, and endpoints.
/// Modules communicate through contracts (integration events, module APIs) — never direct references.
/// </summary>
public interface INacModule
{
    /// <summary>Unique module name used for logging, routing, and configuration scoping.</summary>
    string Name { get; }

    /// <summary>Modules this module depends on. Used for startup ordering and dependency validation.</summary>
    IReadOnlyList<Type> Dependencies => [];

    /// <summary>Register services (handlers, repositories, validators, etc.) into the DI container.</summary>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>Map module endpoints (Minimal API). Called after all modules are registered.</summary>
    void ConfigureEndpoints(IEndpointRouteBuilder routes);

    /// <summary>Optional: configure middleware specific to this module.</summary>
    void ConfigurePipeline(IApplicationBuilder app) { }
}
