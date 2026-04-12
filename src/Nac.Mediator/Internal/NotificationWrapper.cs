using Microsoft.Extensions.DependencyInjection;
using Nac.Messaging;

namespace Nac.Mediator.Internal;

/// <summary>
/// Wraps dispatch for INotification — resolves all handlers and executes sequentially.
/// </summary>
internal sealed class NotificationWrapper<TNotification> : NotificationWrapperBase
    where TNotification : INotification
{
    public override async Task HandleAsync(object notification, IServiceProvider sp, CancellationToken ct)
    {
        var typed = (TNotification)notification;
        var handlers = sp.GetServices<INotificationHandler<TNotification>>();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(typed, ct);
        }
    }
}
