using Nac.Abstractions.Messaging;
using Nac.Identity.Entities;

namespace Nac.Identity.Services;

/// <summary>
/// Publishes identity lifecycle events via <see cref="IEventBus"/>.
/// Consumers inject this after identity operations (registration, email confirm, password reset).
/// Safe to use when IEventBus is not registered (Nac.Messaging not configured) — events are silently skipped.
/// </summary>
public sealed class IdentityEventPublisher
{
    private readonly IEventBus? _eventBus;

    public IdentityEventPublisher(IEventBus? eventBus = null)
        => _eventBus = eventBus;

    /// <summary>Publishes <see cref="UserRegisteredEvent"/> after user account creation.</summary>
    public Task PublishUserRegisteredAsync(NacUser user, string? tenantId = null, CancellationToken ct = default)
        => _eventBus?.PublishAsync(new UserRegisteredEvent(user.Id, user.Email ?? string.Empty, tenantId), ct)
           ?? Task.CompletedTask;

    /// <summary>Publishes <see cref="UserEmailConfirmedEvent"/> after email confirmation.</summary>
    public Task PublishEmailConfirmedAsync(NacUser user, string? tenantId = null, CancellationToken ct = default)
        => _eventBus?.PublishAsync(new UserEmailConfirmedEvent(user.Id, tenantId), ct)
           ?? Task.CompletedTask;

    /// <summary>Publishes <see cref="PasswordResetEvent"/> after password reset.</summary>
    public Task PublishPasswordResetAsync(NacUser user, string? tenantId = null, CancellationToken ct = default)
        => _eventBus?.PublishAsync(new PasswordResetEvent(user.Id, tenantId), ct)
           ?? Task.CompletedTask;
}
