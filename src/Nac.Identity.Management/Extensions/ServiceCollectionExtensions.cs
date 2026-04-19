using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Nac.Core.Abstractions.Permissions;
using Nac.EventBus.Extensions;
using Nac.Identity.Management.Authorization;
using Nac.Identity.Management.Onboarding;
using Nac.Identity.Management.Services;
using Nac.Identity.Permissions;

namespace Nac.Identity.Management.Extensions;

/// <summary>
/// DI registration entry point for the Identity Management module.
/// Registers controllers, validators, permission provider, management services,
/// and authorization policies backed by <see cref="PermissionAuthorizationHandler"/>
/// (calls <c>IPermissionChecker.IsGrantedAsync</c> — NOT a claim check).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Identity Management services, validators, policies, and controllers.
    /// Requires <c>AddNacIdentity</c> to have been called first (provides
    /// <c>IPermissionChecker</c>, <c>PermissionDefinitionManager</c>, etc.).
    /// </summary>
    public static IServiceCollection AddNacIdentityManagement(this IServiceCollection services)
    {
        // Validators — scanned from this assembly.
        services.AddValidatorsFromAssemblyContaining<NacIdentityManagementModule>(
            includeInternalTypes: true);

        // Permission definition provider — feeds PermissionDefinitionManager singleton.
        services.AddTransient<IPermissionDefinitionProvider, IdentityManagementPermissionProvider>();

        // Management services.
        services.AddScoped<MembershipManagementService>();
        services.AddScoped<RoleManagementService>();
        services.AddScoped<UserGrantManagementService>();

        // Tenant onboarding — idempotent role seeding triggered by TenantCreatedEvent.
        services.AddScoped<ITenantOnboardingService, TenantOnboardingService>();

        // Register event handlers from this assembly so AddNacEventBus discovers them.
        services.AddNacEventBus(opts =>
            opts.RegisterHandlersFromAssembly(typeof(NacIdentityManagementModule).Assembly));

        // Authorization policies — one per permission constant, each backed by
        // PermissionRequirement which PermissionAuthorizationHandler resolves via
        // IPermissionChecker.IsGrantedAsync (permissions are NOT in the JWT).
        services.AddAuthorization(opts =>
        {
            foreach (var perm in IdentityManagementPermissions.All)
                opts.AddPolicy(perm, p => p.Requirements.Add(new PermissionRequirement(perm)));
        });

        // Register controllers from this assembly into the MVC application part manager.
        services.AddControllers().ConfigureApplicationPartManager(apm =>
        {
            var assembly = typeof(NacIdentityManagementModule).Assembly;
            if (!apm.ApplicationParts.OfType<AssemblyPart>().Any(p => p.Assembly == assembly))
                apm.ApplicationParts.Add(new AssemblyPart(assembly));
        });

        return services;
    }
}
