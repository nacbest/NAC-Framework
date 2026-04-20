namespace Nac.Identity.Impersonation;

/// <summary>
/// Result of <see cref="IImpersonationRoleProvider.GetImpersonationRoleAsync"/>. Contains
/// the role ids an impersonator runs with inside the target tenant. Pattern A: permissions
/// are never embedded in the JWT — <see cref="Nac.Core.Abstractions.Permissions.IPermissionChecker"/>
/// resolves grants at request time from these <paramref name="RoleIds"/>.
/// </summary>
public sealed record ImpersonationRoleTemplate(string RoleName, IReadOnlyCollection<Guid> RoleIds);
