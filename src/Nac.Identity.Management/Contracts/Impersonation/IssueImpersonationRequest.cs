namespace Nac.Identity.Management.Contracts.Impersonation;

/// <summary>
/// Request body for <c>POST /api/admin/tenants/{tenantId}/impersonate</c>.
/// Reason is validated by <see cref="Nac.Identity.Management.Validators.IssueImpersonationRequestValidator"/>.
/// </summary>
public sealed record IssueImpersonationRequest(string Reason);
