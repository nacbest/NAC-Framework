using Nac.Core.Abstractions.Permissions;

namespace Billing.Permissions;

/// <summary>
/// Declares all permissions owned by the Billing module.
/// Registered as <see cref="IPermissionDefinitionProvider"/> singleton in <c>BillingModule</c>.
/// </summary>
internal sealed class BillingPermissionProvider : IPermissionDefinitionProvider
{
    public const string View   = "Billing.View";
    public const string Manage = "Billing.Manage";

    public void Define(IPermissionDefinitionContext context)
    {
        var group = context.AddGroup("Billing", displayName: "Billing Management");

        group.AddPermission(View,   displayName: "View invoices");
        group.AddPermission(Manage, displayName: "Manage billing");
    }
}
