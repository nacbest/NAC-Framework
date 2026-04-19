namespace Nac.Core.Abstractions.Permissions;

public interface IPermissionDefinitionProvider
{
    void Define(IPermissionDefinitionContext context);
}
