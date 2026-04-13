namespace Nac.Abstractions.MultiTenancy;

/// <summary>
/// Immutable tenant metadata resolved for the current request.
/// </summary>
public sealed record TenantInfo(
    string Id,
    string Name,
    string? ConnectionString = null,
    string? Schema = null,
    IReadOnlyDictionary<string, string>? Properties = null
);
