namespace Nac.Identity.Impersonation;

/// <summary>
/// Consumer-owned port that decides which tenant roles an impersonator runs with.
/// MUST be registered by the consumer — <c>AddNacIdentity</c> fails fast at startup if
/// no implementation is present. Renamed to avoid collision with the unrelated
/// <c>IRoleTemplateProvider</c> (role-template seeding contract).
/// </summary>
public interface IImpersonationRoleProvider
{
    Task<ImpersonationRoleTemplate> GetImpersonationRoleAsync(string tenantId, CancellationToken ct = default);
}
