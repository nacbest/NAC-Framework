namespace Nac.Core.Abstractions.Permissions;

public interface IPermissionDefinitionContext
{
    PermissionGroup AddGroup(string name, string? displayName = null);
    PermissionGroup? GetGroupOrNull(string name);
}
