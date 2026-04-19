using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nac.Core.DataSeeding;
using Nac.Identity.Permissions;
using Nac.Identity.Services;
using Nac.Identity.Users;

namespace ReferenceApp.Host.Seeding;

/// <summary>
/// Seeds the "admin" role and grants it every permission registered across all modules.
/// Also creates a default admin user (dev only — change password in production).
/// Registered as IDataSeeder in AppRootModule.ConfigureServices.
/// </summary>
internal sealed class AdminSeeder(ILogger<AdminSeeder> logger) : IDataSeeder
{
    private const string AdminRole = "admin";
    private const string AdminEmail = "admin@referenceapp.local";
    private const string AdminPassword = "Admin@123456!";

    public async Task SeedAsync(DataSeedContext context)
    {
        var sp = context.ServiceProvider;
        var roleManager = sp.GetRequiredService<RoleManager<NacRole>>();
        var userManager = sp.GetRequiredService<UserManager<NacUser>>();
        var permissionManager = sp.GetRequiredService<PermissionDefinitionManager>();

        // ── Ensure admin role exists ───────────────────────────────────────────
        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            var role = new NacRole(AdminRole, "Full system administrator");
            var roleResult = await roleManager.CreateAsync(role);
            if (!roleResult.Succeeded)
            {
                logger.LogError("Failed to create admin role: {Errors}",
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Created role: {Role}", AdminRole);
        }

        // ── Grant all permissions to the admin role via claims ─────────────────
        var adminRole = await roleManager.FindByNameAsync(AdminRole);
        if (adminRole is null) return;

        var existingClaims = await roleManager.GetClaimsAsync(adminRole);
        var existingPermissions = existingClaims
            .Where(c => c.Type == NacIdentityClaims.Permission)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var permission in permissionManager.GetAll())
        {
            if (existingPermissions.Contains(permission.Name)) continue;

            var claimResult = await roleManager.AddClaimAsync(
                adminRole,
                new System.Security.Claims.Claim(NacIdentityClaims.Permission, permission.Name));

            if (claimResult.Succeeded)
                logger.LogInformation("Granted permission '{Permission}' to role '{Role}'", permission.Name, AdminRole);
            else
                logger.LogWarning("Failed to grant '{Permission}': {Errors}",
                    permission.Name,
                    string.Join(", ", claimResult.Errors.Select(e => e.Description)));
        }

        // ── Ensure default admin user exists (dev convenience) ─────────────────
        var adminUser = await userManager.FindByEmailAsync(AdminEmail);
        if (adminUser is null)
        {
            adminUser = new NacUser(AdminEmail, "System Administrator");
            var userResult = await userManager.CreateAsync(adminUser, AdminPassword);
            if (!userResult.Succeeded)
            {
                logger.LogError("Failed to create admin user: {Errors}",
                    string.Join(", ", userResult.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Created admin user: {Email}", AdminEmail);
        }

        // ── Assign admin role to admin user ────────────────────────────────────
        if (!await userManager.IsInRoleAsync(adminUser, AdminRole))
        {
            await userManager.AddToRoleAsync(adminUser, AdminRole);
            logger.LogInformation("Assigned role '{Role}' to user '{Email}'", AdminRole, AdminEmail);
        }
    }
}
