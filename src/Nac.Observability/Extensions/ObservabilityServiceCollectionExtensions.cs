using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nac.Mediator.Abstractions;

namespace Nac.Observability.Extensions;

/// <summary>
/// DI registration for NAC observability behaviors.
/// </summary>
public static class ObservabilityServiceCollectionExtensions
{
    /// <summary>
    /// Registers logging behaviors for both command and query pipelines.
    /// Should be registered early (outermost behavior) to capture full pipeline duration.
    /// </summary>
    public static IServiceCollection AddNacObservability(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient(
            typeof(ICommandBehavior<,>),
            typeof(LoggingCommandBehavior<,>)));

        services.TryAddEnumerable(ServiceDescriptor.Transient(
            typeof(IQueryBehavior<,>),
            typeof(LoggingQueryBehavior<,>)));

        return services;
    }
}
