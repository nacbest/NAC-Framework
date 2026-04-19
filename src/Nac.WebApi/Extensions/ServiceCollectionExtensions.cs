using Microsoft.Extensions.DependencyInjection;
using Nac.Core.Modularity;

namespace Nac.WebApi.Extensions;

/// <summary>
/// Extension methods for configuring NAC WebApi services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures NAC WebApi options. MUST be called BEFORE
    /// <see cref="Nac.Core.Extensions.ServiceCollectionExtensions.AddNacApplication{TModule}"/>,
    /// otherwise options are read at module-configure time with defaults.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional callback to customize <see cref="NacWebApiOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <c>AddNacApplication</c> was already called. Options would be ignored.
    /// </exception>
    public static IServiceCollection AddNacWebApi(
        this IServiceCollection services,
        Action<NacWebApiOptions>? configure = null)
    {
        // Guard against misordering: if NacApplicationFactory is already registered,
        // modules have already run ConfigureServices with default options.
        if (services.Any(d => d.ServiceType == typeof(NacApplicationFactory)))
        {
            throw new InvalidOperationException(
                "AddNacWebApi() must be called BEFORE AddNacApplication<T>(). " +
                "Modules have already been configured with default NacWebApiOptions; " +
                "calling AddNacWebApi() now would have no effect.");
        }

        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<NacWebApiOptions>(_ => { });

        return services;
    }
}
