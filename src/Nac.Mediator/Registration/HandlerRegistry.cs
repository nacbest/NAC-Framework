using Nac.Abstractions.Messaging;

namespace Nac.Mediator.Registration;

/// <summary>
/// Stores handler metadata built at registration time.
/// Provides fail-fast validation and type lookups used by the mediator at runtime.
/// </summary>
internal sealed class HandlerRegistry
{
    private readonly Dictionary<Type, HandlerDescriptor> _voidCommands = new();
    private readonly Dictionary<Type, HandlerDescriptor> _commands = new();
    private readonly Dictionary<Type, HandlerDescriptor> _queries = new();
    private readonly HashSet<Type> _notifications = [];

    private HandlerRegistry() { }

    /// <summary>
    /// Builds registry from descriptors. Validates uniqueness constraints (fail-fast).
    /// </summary>
    public static HandlerRegistry Build(IReadOnlyList<HandlerDescriptor> descriptors)
    {
        var registry = new HandlerRegistry();

        foreach (var descriptor in descriptors)
        {
            switch (descriptor.Kind)
            {
                case HandlerKind.VoidCommand:
                    if (!registry._voidCommands.TryAdd(descriptor.MessageType, descriptor))
                    {
                        var existing = registry._voidCommands[descriptor.MessageType];
                        throw new InvalidOperationException(
                            $"Duplicate handler for void command '{descriptor.MessageType.Name}': " +
                            $"'{existing.HandlerType.Name}' and '{descriptor.HandlerType.Name}'. " +
                            "Each command must have exactly one handler.");
                    }
                    break;

                case HandlerKind.Command:
                    if (!registry._commands.TryAdd(descriptor.MessageType, descriptor))
                    {
                        var existing = registry._commands[descriptor.MessageType];
                        throw new InvalidOperationException(
                            $"Duplicate handler for command '{descriptor.MessageType.Name}': " +
                            $"'{existing.HandlerType.Name}' and '{descriptor.HandlerType.Name}'. " +
                            "Each command must have exactly one handler.");
                    }
                    break;

                case HandlerKind.Query:
                    if (!registry._queries.TryAdd(descriptor.MessageType, descriptor))
                    {
                        var existing = registry._queries[descriptor.MessageType];
                        throw new InvalidOperationException(
                            $"Duplicate handler for query '{descriptor.MessageType.Name}': " +
                            $"'{existing.HandlerType.Name}' and '{descriptor.HandlerType.Name}'. " +
                            "Each query must have exactly one handler.");
                    }
                    break;

                case HandlerKind.Notification:
                    registry._notifications.Add(descriptor.MessageType);
                    break;
            }
        }

        return registry;
    }

    public void EnsureVoidCommandRegistered(Type commandType)
    {
        if (!_voidCommands.ContainsKey(commandType))
            throw new InvalidOperationException(
                $"No handler registered for void command '{commandType.Name}'. " +
                "Register a handler implementing ICommandHandler<" + commandType.Name + ">.");
    }

    public void EnsureCommandRegistered(Type commandType)
    {
        if (!_commands.ContainsKey(commandType))
            throw new InvalidOperationException(
                $"No handler registered for command '{commandType.Name}'. " +
                "Register a handler implementing ICommandHandler<" + commandType.Name + ", TResult>.");
    }

    public void EnsureQueryRegistered(Type queryType)
    {
        if (!_queries.ContainsKey(queryType))
            throw new InvalidOperationException(
                $"No handler registered for query '{queryType.Name}'. " +
                "Register a handler implementing IQueryHandler<" + queryType.Name + ", TResult>.");
    }

    public Type GetCommandResultType(Type commandType)
    {
        // Extract TResult from ICommand<TResult>
        var iface = commandType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));

        return iface?.GetGenericArguments()[0]
            ?? throw new InvalidOperationException(
                $"Type '{commandType.Name}' does not implement ICommand<TResult>.");
    }

    public Type GetQueryResultType(Type queryType)
    {
        var iface = queryType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));

        return iface?.GetGenericArguments()[0]
            ?? throw new InvalidOperationException(
                $"Type '{queryType.Name}' does not implement IQuery<TResult>.");
    }
}
