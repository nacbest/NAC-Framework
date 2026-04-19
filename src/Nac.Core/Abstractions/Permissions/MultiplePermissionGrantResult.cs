namespace Nac.Core.Abstractions.Permissions;

public sealed class MultiplePermissionGrantResult
{
    private readonly Dictionary<string, bool> _results = new();
    public IReadOnlyDictionary<string, bool> Results => _results;

    public void SetResult(string permission, bool granted) => _results[permission] = granted;

    public bool IsGranted(string permissionName) =>
        _results.TryGetValue(permissionName, out var granted) && granted;

    public bool AllGranted => _results.Values.All(v => v);
}
