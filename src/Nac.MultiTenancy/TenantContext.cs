using Nac.Core.MultiTenancy;

namespace Nac.MultiTenancy;

/// <summary>
/// Scoped implementation of <see cref="ITenantContext"/>.
/// The <see cref="TenantResolutionMiddleware"/> sets <see cref="Current"/>
/// after resolving the tenant from the HTTP request.
/// </summary>
internal sealed class TenantContext : ITenantContext
{
    public bool IsMultiTenant => true;
    public TenantInfo? Current { get; set; }
}
