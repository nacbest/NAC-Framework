using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nac.Core.Modularity;

namespace Nac.Core.Extensions;

/// <summary>
/// Extension methods for bootstrapping the NAC module system.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Discovers all modules starting from <typeparamref name="TModule"/>,
    /// executes their configuration hooks, and registers the application lifetime service.
    /// </summary>
    public static IServiceCollection AddNacApplication<TModule>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TModule : NacModule
    {
        var factory = NacApplicationFactory.Create(typeof(TModule), services, configuration);

        services.AddSingleton(factory);
        services.AddHostedService<NacApplicationLifetime>();

        return services;
    }
}
