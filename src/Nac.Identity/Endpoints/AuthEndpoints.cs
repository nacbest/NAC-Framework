using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Nac.Core.Abstractions.Identity;
using Nac.Identity.Contracts.Auth;
using Nac.Identity.Jwt;
using Nac.Identity.Memberships;
using Nac.Identity.Users;

namespace Nac.Identity.Endpoints;

/// <summary>
/// Minimal API endpoints for authentication: login, switch-tenant, refresh, logout, me, memberships,
/// accept-invitation. Register via <c>app.MapNacAuthEndpoints()</c> in the host.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps all <c>/auth/*</c> endpoints onto the given route builder.
    /// </summary>
    public static IEndpointRouteBuilder MapNacAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/auth").WithTags("Auth");

        g.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .WithMetadata(new AllowTenantlessAttribute());

        g.MapPost("/switch-tenant", SwitchTenantAsync)
            .RequireAuthorization()
            .WithMetadata(new AllowTenantlessAttribute());

        g.MapPost("/refresh", (Delegate)RefreshAsync)
            .AllowAnonymous()
            .WithMetadata(new AllowTenantlessAttribute());

        g.MapPost("/logout", (Delegate)LogoutAsync)
            .RequireAuthorization()
            .WithMetadata(new AllowTenantlessAttribute());

        g.MapGet("/me", MeAsync)
            .RequireAuthorization()
            .WithMetadata(new AllowTenantlessAttribute());

        g.MapGet("/memberships", MembershipsAsync)
            .RequireAuthorization()
            .WithMetadata(new AllowTenantlessAttribute());

        g.MapPost("/accept-invitation", AcceptInvitationAsync)
            .RequireAuthorization()
            .WithMetadata(new AllowTenantlessAttribute());

        return app;
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private static async Task<IResult> LoginAsync(
        LoginRequest req,
        UserManager<NacUser> userManager,
        IMembershipService membershipService,
        JwtTokenService jwtService,
        CancellationToken ct)
    {
        // Identical error for unknown email and wrong password — prevents enumeration.
        var user = await userManager.FindByEmailAsync(req.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, req.Password))
        {
            return Results.Problem(
                title: "Invalid credentials.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: new Dictionary<string, object?> { ["code"] = "NAC_INVALID_CREDENTIALS" });
        }

        var memberships = await membershipService.ListForUserAsync(user.Id, ct);

        var token = jwtService.GenerateToken(
            userId: user.Id,
            tenantId: null,
            email: user.Email ?? string.Empty,
            name: user.FullName,
            roleIds: [],
            isHost: user.IsHost);

        var membershipItems = memberships
            .Select(m => new MembershipListItem(
                TenantId: m.TenantId,
                TenantName: null,        // TenantName not in MembershipSummary (Phase 05 may enrich)
                RoleIds: m.RoleIds,
                Status: m.Status.ToString(),
                IsDefault: m.IsDefault))
            .ToList();

        return Results.Ok(new LoginResponse(
            AccessToken: token,
            User: new LoginUserInfo(user.Id, user.Email ?? string.Empty, user.FullName, user.IsHost),
            Memberships: membershipItems));
    }

    private static async Task<IResult> SwitchTenantAsync(
        SwitchTenantRequest req,
        ICurrentUser currentUser,
        ITenantSwitchService tenantSwitchService,
        CancellationToken ct)
    {
        try
        {
            var result = await tenantSwitchService.IssueTokenForTenantAsync(
                currentUser.Id, req.TenantId, ct);

            return Results.Ok(new SwitchTenantResponse(
                AccessToken: result.AccessToken,
                RoleIds: result.RoleIds,
                ExpiresAt: result.ExpiresAt));
        }
        catch (InvalidOperationException)
        {
            return Results.Problem(
                title: "No active membership in the requested tenant.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["code"] = "NAC_MEMBERSHIP_REQUIRED" });
        }
    }

    private static Task<IResult> RefreshAsync(HttpContext _)
    {
        // Refresh token infrastructure is not implemented in v3.
        // Phase 07 will wire a real refresh token store.
        IResult result = Results.Problem(
            title: "Token refresh is not implemented in this version.",
            statusCode: StatusCodes.Status501NotImplemented,
            extensions: new Dictionary<string, object?> { ["code"] = "NAC_REFRESH_NOT_IMPLEMENTED" });

        return Task.FromResult(result);
    }

    private static Task<IResult> LogoutAsync(HttpContext _)
    {
        // Access tokens are stateless (JWT); client discards locally.
        // Refresh token revocation will be handled in Phase 07.
        return Task.FromResult(Results.NoContent());
    }

    private static Task<IResult> MeAsync(ICurrentUser currentUser)
    {
        var response = new MeResponse(
            Id: currentUser.Id,
            Email: currentUser.Email,
            FullName: currentUser.Name,
            TenantId: currentUser.TenantId,
            RoleIds: currentUser.RoleIds,
            IsHost: currentUser.IsHost);

        return Task.FromResult(Results.Ok(response));
    }

    private static async Task<IResult> MembershipsAsync(
        ICurrentUser currentUser,
        IMembershipService membershipService,
        CancellationToken ct)
    {
        var memberships = await membershipService.ListForUserAsync(currentUser.Id, ct);
        var items = memberships
            .Select(m => new MembershipListItem(
                TenantId: m.TenantId,
                TenantName: null,
                RoleIds: m.RoleIds,
                Status: m.Status.ToString(),
                IsDefault: m.IsDefault))
            .ToList();

        return Results.Ok(items);
    }

    private static async Task<IResult> AcceptInvitationAsync(
        AcceptInvitationRequest req,
        ICurrentUser currentUser,
        IMembershipService membershipService,
        CancellationToken ct)
    {
        try
        {
            await membershipService.AcceptAsync(req.Token, currentUser.Id, ct);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: ex.Message,
                statusCode: StatusCodes.Status404NotFound,
                extensions: new Dictionary<string, object?> { ["code"] = "NAC_INVITATION_NOT_FOUND" });
        }
    }
}
