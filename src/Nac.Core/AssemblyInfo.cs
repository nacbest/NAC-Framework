using System.Runtime.CompilerServices;

// Allow Nac.Identity to access internal members (e.g. PermissionGroup and
// PermissionDefinition constructors) so that PermissionDefinitionContext can
// instantiate them without requiring a public factory API.
[assembly: InternalsVisibleTo("Nac.Identity")]
