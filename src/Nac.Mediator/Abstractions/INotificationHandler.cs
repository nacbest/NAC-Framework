using Nac.Abstractions.Messaging;

namespace Nac.Mediator.Abstractions;

/// <summary>
/// Handler for in-process notifications (one-to-many).
/// Multiple handlers can subscribe to the same notification type.
/// Domain event handlers implement this interface.
/// </summary>
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken ct);
}
