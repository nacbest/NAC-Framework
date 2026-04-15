using System.Collections.Concurrent;
using Nac.Core.Messaging;
using Nac.CQRS.Abstractions;
using Nac.CQRS.Core;
using Nac.CQRS.Registration;

namespace Nac.CQRS.Internal;

/// <summary>
/// Default IMediator implementation. Dispatches commands, queries, and notifications
/// through type-erased wrappers that build the pipeline at runtime.
/// Wrappers are cached after first use for each message type.
/// </summary>
internal sealed class NacMediator : IMediator
{
    private static readonly ConcurrentDictionary<Type, RequestWrapperBase> WrapperCache = new();
    private static readonly ConcurrentDictionary<Type, NotificationWrapperBase> NotificationCache = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly HandlerRegistry _registry;

    public NacMediator(IServiceProvider serviceProvider, HandlerRegistry registry)
    {
        _serviceProvider = serviceProvider;
        _registry = registry;
    }

    public async Task SendAsync(ICommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = command.GetType();
        _registry.EnsureVoidCommandRegistered(commandType);
        var wrapper = WrapperCache.GetOrAdd(commandType, static type =>
        {
            var wrapperType = typeof(VoidCommandWrapper<>).MakeGenericType(type);
            return (RequestWrapperBase)Activator.CreateInstance(wrapperType)!;
        });

        await wrapper.HandleAsync(command, _serviceProvider, ct);
    }

    public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = command.GetType();
        _registry.EnsureCommandRegistered(commandType);
        var wrapper = WrapperCache.GetOrAdd(commandType, static type =>
        {
            var resultType = type.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>))
                .GetGenericArguments()[0];
            var wrapperType = typeof(CommandWrapper<,>).MakeGenericType(type, resultType);
            return (RequestWrapperBase)Activator.CreateInstance(wrapperType)!;
        });

        var result = await wrapper.HandleAsync(command, _serviceProvider, ct);
        return (TResult)result!;
    }

    public async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var queryType = query.GetType();
        _registry.EnsureQueryRegistered(queryType);
        var wrapper = WrapperCache.GetOrAdd(queryType, static type =>
        {
            var resultType = type.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>))
                .GetGenericArguments()[0];
            var wrapperType = typeof(QueryWrapper<,>).MakeGenericType(type, resultType);
            return (RequestWrapperBase)Activator.CreateInstance(wrapperType)!;
        });

        var result = await wrapper.HandleAsync(query, _serviceProvider, ct);
        return (TResult)result!;
    }

    public async Task PublishAsync(INotification notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = notification.GetType();
        var wrapper = NotificationCache.GetOrAdd(notificationType, type =>
        {
            var wrapperType = typeof(NotificationWrapper<>).MakeGenericType(type);
            return (NotificationWrapperBase)Activator.CreateInstance(wrapperType)!;
        });

        await wrapper.HandleAsync(notification, _serviceProvider, ct);
    }
}
