using Microsoft.AspNetCore.Identity;
using Nac.Identity.Permissions;
using Nac.Identity.Services;
using Nac.Identity.Users;

namespace ReferenceApp.IntegrationTests.Infrastructure;

/// <summary>
/// Seeds test users and roles after each Respawn reset.
/// Idempotent: checks existence before creating.
///
/// Permission mechanism discovery (Phase 06):
///   AdminSeeder adds permissions as ROLE claims (roleManager.AddClaimAsync).
///   JwtTokenService.GenerateTokenAsync only reads USER claims via userManager.GetClaimsAsync.
///   Role claims are NOT expanded into the JWT by default.
///   PermissionChecker reads permission claims from the JWT ClaimsPrincipal.
///   Therefore: permissions must be granted as USER claims so JwtTokenService embeds them.
///
///   Workaround: grant permissions directly on admin users as user claims.
///   Host AdminSeeder uses role claims — that works only if JwtTokenService is extended
///   to aggregate role claims (framework gap). For tests we grant at user level.
///
/// Users seeded:
///   - admin@test.com / Pass123!     — role "admin" + all permissions as user claims
///   - noperm@test.com / Pass123!    — no role, no permissions
///   - admin-b@test.com / Pass123!   — role "admin" + all permissions, tenantId="tenant-b"
/// </summary>
public static class TestDataSeeder
{
    public const string AdminEmail    = "admin@test.com";
    public const string NoPermEmail   = "noperm@test.com";
    public const string TenantBAdmin  = "admin-b@test.com";
    public const string TestPassword  = "Pass123!";
    public const string DefaultTenant = "default";
    public const string TenantB       = "tenant-b";
    public const string AdminRole     = "admin";

    public static async Task SeedAsync(IServiceProvider sp)
    {
        var userManager       = sp.GetRequiredService<UserManager<NacUser>>();
        var roleManager       = sp.GetRequiredService<RoleManager<NacRole>>();
        var permissionManager = sp.GetRequiredService<PermissionDefinitionManager>();

        var allPermissions = permissionManager.GetAll().Select(p => p.Name).ToList();

        // ── Admin role (for role-based checks, even if permissions go on user) ──
        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            var role = new NacRole(AdminRole, "Integration test admin");
            await roleManager.CreateAsync(role);
        }

        // ── Admin user (default tenant) ────────────────────────────────────────
        await EnsureUserWithPermissionsAsync(userManager, AdminEmail, DefaultTenant, AdminRole, allPermissions);

        // ── No-permission user (no role, no permission claims) ─────────────────
        await EnsureUserAsync(userManager, NoPermEmail, DefaultTenant, role: null);

        // ── Tenant-B admin (cross-tenant isolation tests) ──────────────────────
        await EnsureUserWithPermissionsAsync(userManager, TenantBAdmin, TenantB, AdminRole, allPermissions);
    }

    /// <summary>
    /// Creates user + assigns role + grants all permissions as direct USER claims.
    /// JwtTokenService reads user claims (not role claims) when building the JWT,
    /// so permission checks in PermissionChecker succeed only when claims are on the user.
    /// </summary>
    private static async Task EnsureUserWithPermissionsAsync(
        UserManager<NacUser> userManager,
        string email,
        string tenantId,
        string role,
        IReadOnlyList<string> permissions)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new NacUser(email, tenantId) { FullName = email };
            var result = await userManager.CreateAsync(user, TestPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to create user {email}: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Role assignment.
        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);

        // Grant permissions as user-level claims so JwtTokenService embeds them in JWT.
        var existingClaims = await userManager.GetClaimsAsync(user);
        var existingPermissionValues = existingClaims
            .Where(c => c.Type == NacIdentityClaims.Permission)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var perm in permissions)
        {
            if (existingPermissionValues.Contains(perm)) continue;
            await userManager.AddClaimAsync(
                user,
                new System.Security.Claims.Claim(NacIdentityClaims.Permission, perm));
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<NacUser> userManager,
        string email,
        string tenantId,
        string? role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new NacUser(email, tenantId) { FullName = email };
            var result = await userManager.CreateAsync(user, TestPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to create test user {email}: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        if (role is not null && !await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);
    }
}
