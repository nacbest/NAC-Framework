using System.Collections.Frozen;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Nac.Cqrs.Dispatching;
using Nac.Cqrs.Pipeline;

namespace Nac.Cqrs.Extensions;

/// <summary>
/// Options for configuring the NAC CQRS dispatcher at startup.
/// </summary>
public sealed class NacCqrsOptions
{
    internal List<Assembly> Assemblies { get; } = [];

    /// <summary>
    /// Deferred pipeline behavior registrations applied inside <see cref="ServiceCollectionExtensions.AddNacCqrs"/>.
    /// Stored as actions so behaviors are registered in the caller's declared order,
    /// which the <c>Sender</c> reverses to make the first-registered the outermost wrapper.
    /// </summary>
    internal List<Action<IServiceCollection>> BehaviorRegistrations { get; } = [];

    /// <summary>
    /// Adds <paramref name="assembly"/> to the set of assemblies scanned for
    /// command and query handler implementations.
    /// </summary>
    /// <param name="assembly">Assembly containing handler classes.</param>
    /// <returns>The same <see cref="NacCqrsOptions"/> instance for chaining.</returns>
    public NacCqrsOptions RegisterHandlersFromAssembly(Assembly assembly)
    {
        Assemblies.Add(assembly);
        return this;
    }

    /// <summary>
    /// Registers <see cref="ValidationBehavior{TRequest,TResponse}"/> into the pipeline.
    /// Should be called first so validation is the outermost wrapper.
    /// Requires at least one <c>FluentValidation.IValidator&lt;TRequest&gt;</c> registered in DI
    /// to have any effect; requests with no validators pass through silently.
    /// </summary>
    /// <returns>The same <see cref="NacCqrsOptions"/> instance for chaining.</returns>
    public NacCqrsOptions AddValidationBehavior()
    {
        BehaviorRegistrations.Add(services =>
            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(ValidationBehavior<,>)));
        return this;
    }

    /// <summary>
    /// Registers <see cref="LoggingBehavior{TRequest,TResponse}"/> into the pipeline.
    /// Logs request start, normal completion with elapsed time, and slow-request warnings.
    /// </summary>
    /// <returns>The same <see cref="NacCqrsOptions"/> instance for chaining.</returns>
    public NacCqrsOptions AddLoggingBehavior()
    {
        BehaviorRegistrations.Add(services =>
            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(LoggingBehavior<,>)));
        return this;
    }

    /// <summary>
    /// Registers <see cref="CachingBehavior{TRequest,TResponse}"/> into the pipeline.
    /// Only caches results for requests that implement <c>ICacheableQuery</c>.
    /// Requires <c>INacCache</c> to be registered in DI (via <c>AddNacCaching()</c>).
    /// </summary>
    /// <returns>The same <see cref="NacCqrsOptions"/> instance for chaining.</returns>
    public NacCqrsOptions AddCachingBehavior()
    {
        BehaviorRegistrations.Add(services =>
            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(CachingBehavior<,>)));
        return this;
    }

    /// <summary>
    /// Registers <see cref="TransactionBehavior{TRequest,TResponse}"/> into the pipeline.
    /// Only flushes the unit of work for requests that implement <c>ITransactionalCommand</c>.
    /// Requires <c>IUnitOfWork</c> to be registered in DI.
    /// Should be called last so it is the innermost wrapper, closest to the handler.
    /// </summary>
    /// <returns>The same <see cref="NacCqrsOptions"/> instance for chaining.</returns>
    public NacCqrsOptions AddTransactionBehavior()
    {
        BehaviorRegistrations.Add(services =>
            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(TransactionBehavior<,>)));
        return this;
    }
}

/// <summary>
/// Extension methods for registering the NAC CQRS infrastructure into an
/// <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the CQRS dispatcher (<see cref="ISender"/>), scans the configured assemblies
    /// for command/query handlers, and wires up any registered pipeline behaviors.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="configure">Callback to configure <see cref="NacCqrsOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddNacCqrs(opts =>
    ///     opts.RegisterHandlersFromAssembly(typeof(MyHandler).Assembly));
    /// </code>
    /// </example>
    public static IServiceCollection AddNacCqrs(
        this IServiceCollection services,
        Action<NacCqrsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new NacCqrsOptions();
        configure(options);

        // Scan assemblies, register handlers as scoped, and build the frozen registry.
        var registry = HandlerRegistryExtensions.RegisterHandlersAndBuildRegistry(
            services,
            options.Assemblies);

        // Register the frozen dictionary as a singleton — it is immutable after startup.
        services.AddSingleton<FrozenDictionary<Type, RequestHandlerBase>>(registry);

        // Sender is scoped so it inherits the same scope as its resolved handlers.
        services.AddScoped<ISender, Sender>();

        // Apply pipeline behavior registrations in declared order.
        // Sender reverses them at dispatch time, making the first-registered the outermost wrapper.
        foreach (var registration in options.BehaviorRegistrations)
            registration(services);

        return services;
    }
}
