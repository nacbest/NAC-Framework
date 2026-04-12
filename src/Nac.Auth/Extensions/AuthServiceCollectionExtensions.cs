using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nac.Mediator.Abstractions;

namespace Nac.Auth.Extensions;

/// <summary>
/// DI registration for NAC authorization behaviors.
/// </summary>
public static class AuthServiceCollectionExtensions
{
    /// <summary>
    /// Registers authorization behaviors for both command and query pipelines.
    /// Commands/queries implementing <see cref="Nac.Abstractions.Auth.IRequirePermission"/>
    /// will be checked against the current user's permissions.
    /// </summary>
    public static IServiceCollection AddNacAuthorization(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient(
            typeof(ICommandBehavior<,>),
            typeof(AuthorizationCommandBehavior<,>)));

        services.TryAddEnumerable(ServiceDescriptor.Transient(
            typeof(IQueryBehavior<,>),
            typeof(AuthorizationQueryBehavior<,>)));

        return services;
    }
}
