using Microsoft.Extensions.DependencyInjection;

namespace Nac.Caching.Extensions;

/// <summary>
/// Extension methods for registering NAC caching services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="INacCache"/> and its dependencies with the DI container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">
    /// Optional delegate to customise <see cref="NacCacheOptions"/> (e.g. default expiration).
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddNacCaching(
        this IServiceCollection services,
        Action<NacCacheOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<NacCacheOptions>(_ => { });

        services.AddScoped<INacCache, NacCache>();
        return services;
    }
}
