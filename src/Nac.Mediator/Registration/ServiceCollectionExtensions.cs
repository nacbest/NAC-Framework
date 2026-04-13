using Microsoft.Extensions.DependencyInjection;
using Nac.Mediator.Abstractions;
using Nac.Mediator.Core;

namespace Nac.Mediator.Registration;

/// <summary>
/// Extension methods for registering the NAC mediator in the DI container.
/// </summary>
public static class MediatorServiceCollectionExtensions
{
    /// <summary>
    /// Adds the NAC mediator with handler scanning and behavior pipeline configuration.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddNacMediator(options =>
    /// {
    ///     options.RegisterHandlersFromAssemblyContaining&lt;Program&gt;();
    ///     options.AddCommandBehavior(typeof(LoggingBehavior&lt;,&gt;));
    ///     options.AddCommandBehavior(typeof(ValidationBehavior&lt;,&gt;));
    ///     options.AddQueryBehavior(typeof(CachingBehavior&lt;,&gt;));
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddNacMediator(
        this IServiceCollection services,
        Action<MediatorOptions> configure)
    {
        var options = new MediatorOptions();
        configure(options);

        // Scan assemblies and collect handler descriptors
        var descriptors = new List<HandlerDescriptor>();
        foreach (var assembly in options.AssembliesToScan)
        {
            descriptors.AddRange(HandlerScanner.ScanAssembly(assembly));
        }

        // Build registry (validates uniqueness — fail-fast)
        var registry = HandlerRegistry.Build(descriptors);
        services.AddSingleton(registry);

        // Register each handler in DI
        foreach (var descriptor in descriptors)
        {
            services.AddTransient(descriptor.ServiceInterfaceType, descriptor.HandlerType);
        }

        // Register command behaviors in order (open generics)
        foreach (var behaviorType in options.CommandBehaviorTypes)
        {
            services.AddTransient(typeof(ICommandBehavior<,>), behaviorType);
        }

        // Register query behaviors in order (open generics)
        foreach (var behaviorType in options.QueryBehaviorTypes)
        {
            services.AddTransient(typeof(IQueryBehavior<,>), behaviorType);
        }

        // Register mediator
        services.AddScoped<IMediator, Internal.NacMediator>();

        return services;
    }

    /// <summary>
    /// Adds the NAC mediator with handlers from the assembly containing <typeparamref name="TMarker"/>.
    /// No pipeline behaviors are registered — add them later or use the overload with options.
    /// </summary>
    public static IServiceCollection AddNacMediator<TMarker>(this IServiceCollection services)
        => services.AddNacMediator(o => o.RegisterHandlersFromAssemblyContaining<TMarker>());
}
