using Microsoft.AspNetCore.Routing;

namespace Nac.WebApi.Modularity;

/// <summary>
/// Implement on non-static classes to auto-register endpoints.
/// NacFrameworkBuilder scans module assemblies for implementations at startup.
/// Must be a sealed class (not static) — static classes are IsAbstract in reflection and won't be discovered.
/// </summary>
public interface IEndpointMapper
{
    void MapEndpoints(IEndpointRouteBuilder routes);
}
