namespace Nac.Core.Messaging;

/// <summary>
/// Marker interface for in-process notifications (one-to-many).
/// Domain events are dispatched as notifications after UnitOfWork commit.
/// Multiple handlers can subscribe to a single notification type.
/// </summary>
public interface INotification;
