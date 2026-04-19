using Nac.Core.Abstractions.Permissions;

namespace Orders.Permissions;

/// <summary>
/// Declares all permissions owned by the Orders module.
/// Registered as <see cref="IPermissionDefinitionProvider"/> singleton in <c>OrdersModule</c>.
/// </summary>
internal sealed class OrderPermissionProvider : IPermissionDefinitionProvider
{
    public const string View   = "Orders.View";
    public const string Create = "Orders.Create";
    public const string Edit   = "Orders.Edit";
    public const string Delete = "Orders.Delete";

    public void Define(IPermissionDefinitionContext context)
    {
        var group = context.AddGroup("Orders", displayName: "Orders Management");

        group.AddPermission(View,   displayName: "View orders");
        group.AddPermission(Create, displayName: "Create orders");
        group.AddPermission(Edit,   displayName: "Edit orders");
        group.AddPermission(Delete, displayName: "Delete orders");
    }
}
