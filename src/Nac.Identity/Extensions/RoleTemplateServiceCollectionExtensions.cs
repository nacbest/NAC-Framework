using Microsoft.Extensions.DependencyInjection;
using Nac.Identity.RoleTemplates;

namespace Nac.Identity.Extensions;

/// <summary>
/// Extension methods for registering the role template system into the DI container.
/// Call <see cref="AddNacRoleTemplates"/> after <c>AddNacIdentity</c> so that
/// <c>PermissionDefinitionManager</c> is already registered when the seeder validates
/// permission names.
/// </summary>
public static class RoleTemplateServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="RoleTemplateDefinitionManager"/>, the default
    /// <see cref="IRoleTemplateProvider"/> implementations, and the
    /// <see cref="RoleTemplateSeeder"/> hosted service.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddNacRoleTemplates(this IServiceCollection services)
    {
        // Manager is a singleton — built once from all registered providers.
        services.AddSingleton<RoleTemplateDefinitionManager>();

        // Default built-in provider (owner / admin / member / guest).
        services.AddTransient<IRoleTemplateProvider, DefaultRoleTemplateProvider>();

        // Seeder runs after app start; uses IServiceScopeFactory to resolve scoped DbContext.
        services.AddHostedService<RoleTemplateSeeder>();

        return services;
    }

    /// <summary>
    /// Registers an additional <see cref="IRoleTemplateProvider"/> so that module-defined
    /// templates are picked up by the <see cref="RoleTemplateDefinitionManager"/>.
    /// </summary>
    /// <typeparam name="TProvider">Concrete provider type.</typeparam>
    public static IServiceCollection AddRoleTemplateProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, IRoleTemplateProvider
    {
        services.AddTransient<IRoleTemplateProvider, TProvider>();
        return services;
    }
}
