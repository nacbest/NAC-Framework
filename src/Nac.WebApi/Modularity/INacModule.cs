using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nac.WebApi.Modularity;

/// <summary>
/// Represents a self-contained module in the NAC framework.
/// Each module owns its own domain, application logic, infrastructure, and endpoints.
/// Modules communicate through contracts (integration events, module APIs) — never direct references.
/// Use [DependsOn] attribute for dependency declaration. Endpoints are auto-discovered via IEndpointMapper.
/// </summary>
public interface INacModule
{
    /// <summary>Unique module name used for logging, routing, and configuration scoping.</summary>
    string Name { get; }

    /// <summary>Register services (handlers, repositories, validators, etc.) into the DI container.</summary>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
}
