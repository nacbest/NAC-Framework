using System.Reflection;
using Nac.CQRS.Abstractions;

namespace Nac.CQRS.Registration;

/// <summary>
/// Configuration for the NAC mediator. Allows registering handler assemblies
/// and pipeline behaviors in explicit order.
/// </summary>
public sealed class MediatorOptions
{
    internal List<Assembly> AssembliesToScan { get; } = [];
    internal List<Type> CommandBehaviorTypes { get; } = [];
    internal List<Type> QueryBehaviorTypes { get; } = [];

    /// <summary>
    /// Registers all handlers found in the specified assembly.
    /// Scans for ICommandHandler, IQueryHandler, and INotificationHandler implementations.
    /// </summary>
    public MediatorOptions RegisterHandlersFromAssembly(Assembly assembly)
    {
        AssembliesToScan.Add(assembly);
        return this;
    }

    /// <summary>
    /// Registers all handlers from the assembly containing <typeparamref name="TMarker"/>.
    /// </summary>
    public MediatorOptions RegisterHandlersFromAssemblyContaining<TMarker>()
        => RegisterHandlersFromAssembly(typeof(TMarker).Assembly);

    /// <summary>
    /// Adds a command pipeline behavior. Behaviors execute in registration order
    /// (first registered = outermost in the pipeline chain).
    /// The type must be an open generic implementing ICommandBehavior&lt;,&gt;.
    /// </summary>
    public MediatorOptions AddCommandBehavior(Type openGenericBehaviorType)
    {
        ValidateOpenGeneric(openGenericBehaviorType, typeof(ICommandBehavior<,>), "ICommandBehavior<,>");
        CommandBehaviorTypes.Add(openGenericBehaviorType);
        return this;
    }

    /// <summary>
    /// Adds a query pipeline behavior. Behaviors execute in registration order
    /// (first registered = outermost in the pipeline chain).
    /// The type must be an open generic implementing IQueryBehavior&lt;,&gt;.
    /// </summary>
    public MediatorOptions AddQueryBehavior(Type openGenericBehaviorType)
    {
        ValidateOpenGeneric(openGenericBehaviorType, typeof(IQueryBehavior<,>), "IQueryBehavior<,>");
        QueryBehaviorTypes.Add(openGenericBehaviorType);
        return this;
    }

    private static void ValidateOpenGeneric(Type type, Type expectedInterface, string interfaceName)
    {
        if (!type.IsGenericTypeDefinition)
            throw new ArgumentException(
                $"Type '{type.Name}' must be an open generic type definition. " +
                $"Use typeof({type.Name}<,>) instead of typeof({type.Name}<SomeType, SomeResult>).",
                nameof(type));

        var implementsInterface = type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == expectedInterface);

        if (!implementsInterface)
        {
            // Check base class interfaces too (for abstract classes)
            var baseType = type.BaseType;
            while (baseType is not null && !implementsInterface)
            {
                implementsInterface = baseType.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == expectedInterface);
                baseType = baseType.BaseType;
            }
        }

        if (!implementsInterface)
            throw new ArgumentException(
                $"Type '{type.Name}' does not implement {interfaceName}.",
                nameof(type));
    }
}
