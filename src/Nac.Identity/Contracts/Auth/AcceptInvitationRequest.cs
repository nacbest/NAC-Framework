namespace Nac.Identity.Contracts.Auth;

/// <summary>
/// Payload for <c>POST /auth/accept-invitation</c>.
/// </summary>
public sealed record AcceptInvitationRequest(string Token);
