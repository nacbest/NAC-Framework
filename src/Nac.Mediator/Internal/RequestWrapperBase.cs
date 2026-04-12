namespace Nac.Mediator.Internal;

/// <summary>Base class for type-erased request dispatch wrappers.</summary>
internal abstract class RequestWrapperBase
{
    public abstract Task<object?> HandleAsync(object request, IServiceProvider sp, CancellationToken ct);
}

/// <summary>Base class for type-erased notification dispatch wrappers.</summary>
internal abstract class NotificationWrapperBase
{
    public abstract Task HandleAsync(object notification, IServiceProvider sp, CancellationToken ct);
}
