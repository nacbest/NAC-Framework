using Nac.Core.Abstractions.Permissions;

namespace Nac.Identity.Permissions.Host;

/// <summary>Registers the Host permission group.</summary>
public sealed class HostPermissionProvider : IPermissionDefinitionProvider
{
    /// <inheritdoc/>
    public void Define(IPermissionDefinitionContext context)
    {
        var host = context.AddGroup("Host", "Host Platform");
        host.AddPermission(HostPermissions.AccessAllTenants, "Access all tenants");
    }
}
