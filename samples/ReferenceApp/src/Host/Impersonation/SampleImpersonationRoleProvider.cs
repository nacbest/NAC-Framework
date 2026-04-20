using Nac.Identity.Impersonation;

namespace ReferenceApp.Host.Impersonation;

/// <summary>
/// Demo impersonation role provider for ReferenceApp. Returns a hard-coded
/// "support-operator" template with a fixed role id. Real consumers would
/// look up the tenant's pre-seeded support role from their identity DB.
/// </summary>
public sealed class SampleImpersonationRoleProvider : IImpersonationRoleProvider
{
    private static readonly Guid SupportOperatorRoleId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Task<ImpersonationRoleTemplate> GetImpersonationRoleAsync(
        string tenantId,
        CancellationToken ct = default)
    {
        var template = new ImpersonationRoleTemplate(
            RoleName: "support-operator",
            RoleIds: new[] { SupportOperatorRoleId });

        return Task.FromResult(template);
    }
}
