using Nac.Core.Messaging;
using Nac.Identity.Entities;

namespace Nac.Identity.Services;

/// <summary>
/// Publishes identity lifecycle events via <see cref="IEventBus"/>.
/// Generic over TUser to support custom user types extending <see cref="NacIdentityUser"/>.
/// Safe to use when IEventBus is not registered (Nac.Messaging not configured) — events are silently skipped.
/// </summary>
public class IdentityEventPublisher<TUser>
    where TUser : NacIdentityUser
{
    private readonly IEventBus? _eventBus;

    public IdentityEventPublisher(IEventBus? eventBus = null)
        => _eventBus = eventBus;

    /// <summary>Publishes <see cref="UserRegisteredEvent"/> after user account creation.</summary>
    public Task PublishUserRegisteredAsync(TUser user, string? tenantId = null, CancellationToken ct = default)
        => _eventBus?.PublishAsync(new UserRegisteredEvent(user.Id, user.Email ?? string.Empty, tenantId), ct)
           ?? Task.CompletedTask;

    /// <summary>Publishes <see cref="UserEmailConfirmedEvent"/> after email confirmation.</summary>
    public Task PublishEmailConfirmedAsync(TUser user, string? tenantId = null, CancellationToken ct = default)
        => _eventBus?.PublishAsync(new UserEmailConfirmedEvent(user.Id, tenantId), ct)
           ?? Task.CompletedTask;

    /// <summary>Publishes <see cref="PasswordResetEvent"/> after password reset.</summary>
    public Task PublishPasswordResetAsync(TUser user, string? tenantId = null, CancellationToken ct = default)
        => _eventBus?.PublishAsync(new PasswordResetEvent(user.Id, tenantId), ct)
           ?? Task.CompletedTask;
}

/// <summary>
/// Non-generic convenience alias using <see cref="NacIdentityUser"/> directly.
/// </summary>
public sealed class IdentityEventPublisher : IdentityEventPublisher<NacIdentityUser>
{
    public IdentityEventPublisher(IEventBus? eventBus = null) : base(eventBus)
    {
    }
}
