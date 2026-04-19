using Nac.Core.Abstractions.Permissions;

namespace Nac.Testing.Fakes;

public sealed class FakePermissionChecker : IPermissionChecker
{
    private bool _grantAll;
    private readonly HashSet<string> _granted = [];

    public Task<bool> IsGrantedAsync(string permissionName, CancellationToken ct = default) =>
        Task.FromResult(_grantAll || _granted.Contains(permissionName));

    public Task<bool> IsGrantedAsync(Guid userId, string permissionName,
        string? tenantId = null, CancellationToken ct = default) =>
        Task.FromResult(_grantAll || _granted.Contains(permissionName));

    public Task<bool> IsGrantedAsync(string permissionName, string resourceType,
        string resourceId, CancellationToken ct = default) =>
        Task.FromResult(_grantAll || _granted.Contains(permissionName));

    public Task<MultiplePermissionGrantResult> IsGrantedAsync(string[] permissionNames,
        CancellationToken ct = default)
    {
        var result = new MultiplePermissionGrantResult();
        foreach (var name in permissionNames)
            result.SetResult(name, _grantAll || _granted.Contains(name));
        return Task.FromResult(result);
    }

    public static FakePermissionChecker GrantAll() => new() { _grantAll = true };

    public static FakePermissionChecker WithPermissions(params string[] permissions)
    {
        var checker = new FakePermissionChecker();
        foreach (var p in permissions) checker._granted.Add(p);
        return checker;
    }
}
