using System.Reflection;
using Nac.CQRS.Abstractions;

namespace Nac.CQRS.Registration;

/// <summary>
/// Scans assemblies for types implementing handler interfaces.
/// Discovers ICommandHandler, IQueryHandler, and INotificationHandler implementations.
/// </summary>
internal static class HandlerScanner
{
    private static readonly Type VoidCommandHandlerOpen = typeof(ICommandHandler<>);
    private static readonly Type CommandHandlerOpen = typeof(ICommandHandler<,>);
    private static readonly Type QueryHandlerOpen = typeof(IQueryHandler<,>);
    private static readonly Type NotificationHandlerOpen = typeof(INotificationHandler<>);

    public static IEnumerable<HandlerDescriptor> ScanAssembly(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t is not null).ToArray()!;
        }

        foreach (var type in types)
        {
            if (type is { IsAbstract: true } or { IsInterface: true })
                continue;

            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;

                var genericDef = iface.GetGenericTypeDefinition();
                var args = iface.GetGenericArguments();

                if (genericDef == VoidCommandHandlerOpen)
                {
                    yield return new(args[0], type, iface, HandlerKind.VoidCommand);
                }
                else if (genericDef == CommandHandlerOpen)
                {
                    yield return new(args[0], type, iface, HandlerKind.Command, args[1]);
                }
                else if (genericDef == QueryHandlerOpen)
                {
                    yield return new(args[0], type, iface, HandlerKind.Query, args[1]);
                }
                else if (genericDef == NotificationHandlerOpen)
                {
                    yield return new(args[0], type, iface, HandlerKind.Notification);
                }
            }
        }
    }
}
