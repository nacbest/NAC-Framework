namespace Nac.Core.Messaging;

/// <summary>Published after a new user account is created.</summary>
public sealed record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string? TenantId) : IntegrationEvent;

/// <summary>Published after a user confirms their email address.</summary>
public sealed record UserEmailConfirmedEvent(
    Guid UserId,
    string? TenantId) : IntegrationEvent;

/// <summary>Published after a user successfully resets their password.</summary>
public sealed record PasswordResetEvent(
    Guid UserId,
    string? TenantId) : IntegrationEvent;
