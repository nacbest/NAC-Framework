namespace Nac.Identity.Contracts.Auth;

/// <summary>
/// Payload for <c>POST /auth/login</c>.
/// </summary>
public sealed record LoginRequest(string Email, string Password);
