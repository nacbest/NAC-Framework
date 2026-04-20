namespace Nac.Identity.Management.Contracts.Impersonation;

/// <summary>
/// Response body for <c>POST /api/admin/tenants/{tenantId}/impersonate</c>.
/// Contains the minted short-lived token, its expiry, and the audit session id.
/// </summary>
public sealed record IssueImpersonationResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid SessionId);
