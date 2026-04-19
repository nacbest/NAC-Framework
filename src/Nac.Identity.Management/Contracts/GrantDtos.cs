namespace Nac.Identity.Management.Contracts;

/// <summary>Grant a single permission to a role or user.</summary>
public sealed record GrantRequest(string PermissionName);

/// <summary>Replace the full set of grants on a role (bulk replace = single invalidation).</summary>
public sealed record BulkGrantsRequest(IReadOnlyList<string> PermissionNames);

/// <summary>Single grant entry returned by list endpoints.</summary>
public sealed record GrantDto(string PermissionName, string ProviderName, string ProviderKey, DateTime CreatedAt);
