using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nac.CQRS.Abstractions;

namespace Nac.Caching.Extensions;

/// <summary>
/// DI registration for NAC caching behaviors.
/// Requires an <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>
/// implementation to be registered (e.g., in-memory, Redis).
/// </summary>
public static class CachingServiceCollectionExtensions
{
    /// <summary>
    /// Registers caching query behavior and command cache invalidation behavior.
    /// Also adds in-memory distributed cache as a fallback if none is registered.
    /// </summary>
    public static IServiceCollection AddNacCaching(this IServiceCollection services)
    {
        // Fallback: in-memory cache if no IDistributedCache is registered
        services.AddDistributedMemoryCache();

        services.TryAddEnumerable(ServiceDescriptor.Transient(
            typeof(IQueryBehavior<,>),
            typeof(CachingQueryBehavior<,>)));

        services.TryAddEnumerable(ServiceDescriptor.Transient(
            typeof(ICommandBehavior<,>),
            typeof(CacheInvalidationBehavior<,>)));

        return services;
    }
}
