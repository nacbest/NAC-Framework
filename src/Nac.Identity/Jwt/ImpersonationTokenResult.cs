namespace Nac.Identity.Jwt;

/// <summary>
/// Output of <see cref="JwtTokenService.GenerateImpersonationToken"/>. <paramref name="Jti"/>
/// is the unique token id used by the revocation blacklist; <paramref name="ExpiresAt"/> is UTC.
/// </summary>
public sealed record ImpersonationTokenResult(string Token, string Jti, DateTime ExpiresAt);
