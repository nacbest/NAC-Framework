namespace Nac.Identity.RoleTemplates;

/// <summary>
/// Contract for modules that contribute role templates at startup. Analogous to
/// <c>IPermissionDefinitionProvider</c> — implementations are registered with DI and
/// collected by <see cref="RoleTemplateDefinitionManager"/> during construction.
/// </summary>
public interface IRoleTemplateProvider
{
    /// <summary>
    /// Defines one or more role templates via <paramref name="context"/>.
    /// Called once at application startup; implementations must be deterministic.
    /// </summary>
    void Define(IRoleTemplateContext context);
}
