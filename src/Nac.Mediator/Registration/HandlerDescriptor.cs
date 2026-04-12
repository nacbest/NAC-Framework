namespace Nac.Mediator.Registration;

/// <summary>Metadata about a discovered handler.</summary>
internal sealed record HandlerDescriptor(
    Type MessageType,
    Type HandlerType,
    Type ServiceInterfaceType,
    HandlerKind Kind,
    Type? ResultType = null
);

internal enum HandlerKind
{
    VoidCommand,
    Command,
    Query,
    Notification
}
