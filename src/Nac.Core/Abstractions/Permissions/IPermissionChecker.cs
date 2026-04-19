namespace Nac.Core.Abstractions.Permissions;

public interface IPermissionChecker
{
    Task<bool> IsGrantedAsync(string permissionName);
    Task<bool> IsGrantedAsync(Guid userId, string permissionName);
    Task<MultiplePermissionGrantResult> IsGrantedAsync(string[] permissionNames);
}
