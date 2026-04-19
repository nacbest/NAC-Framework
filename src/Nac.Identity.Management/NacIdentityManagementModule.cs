using Nac.Core.Modularity;
using Nac.Identity.Management.Extensions;
using Nac.MultiTenancy;

namespace Nac.Identity.Management;

/// <summary>
/// NAC Framework Identity Management module — opinionated admin REST API for users,
/// memberships, roles, and permission grants on top of <see cref="NacIdentityModule"/>.
/// Service registration is delegated to
/// <c>IServiceCollection.AddNacIdentityManagement()</c> (see
/// <see cref="ServiceCollectionExtensions"/>).
/// </summary>
[DependsOn(typeof(NacIdentityModule))]
[DependsOn(typeof(NacMultiTenancyModule))]
public sealed class NacIdentityManagementModule : NacModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddNacIdentityManagement();
    }
}
