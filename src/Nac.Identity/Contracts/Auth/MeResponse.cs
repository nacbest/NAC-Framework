namespace Nac.Identity.Contracts.Auth;

/// <summary>
/// Response for <c>GET /auth/me</c>. Works for both tenantless and tenant-scoped sessions.
/// </summary>
public sealed record MeResponse(
    Guid Id,
    string? Email,
    string? FullName,
    string? TenantId,
    IReadOnlyList<Guid> RoleIds,
    bool IsHost,
    Guid? ImpersonatorId);
