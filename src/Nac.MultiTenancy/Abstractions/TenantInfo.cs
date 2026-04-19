namespace Nac.MultiTenancy.Abstractions;

public sealed class TenantInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? ConnectionString { get; init; }
    public bool IsActive { get; init; } = true;
    public Dictionary<string, string?> Properties { get; init; } = [];
}
