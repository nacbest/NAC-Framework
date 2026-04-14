# Auth Endpoints Reference

Complete code patterns for 8 authentication endpoints.

## Endpoints Overview

| Endpoint | Method | Auth | Request DTO | Response |
|----------|--------|------|-------------|----------|
| `/auth/login` | POST | No | LoginRequest | TokenResponse |
| `/auth/register` | POST | No | RegisterRequest | `{userId, message}` |
| `/auth/tenants` | GET | Yes | - | TenantListResponse |
| `/auth/select-tenant` | POST | Yes | SelectTenantRequest | TokenResponse |
| `/auth/refresh` | POST | No | RefreshRequest | TokenResponse |
| `/auth/logout` | POST | Yes | `{refreshToken?}` | `{success}` |
| `/auth/change-password` | POST | Yes | ChangePasswordRequest | `{success}` |
| `/auth/forgot-password` | POST | No | ForgotPasswordRequest | `{message}` |

---

## DTOs

### LoginRequest.cs
```csharp
namespace {Namespace}.Auth.DTOs;

public sealed record LoginRequest(
    string Email,
    string Password);
```

### RegisterRequest.cs
```csharp
namespace {Namespace}.Auth.DTOs;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string? DisplayName);
```

### RefreshRequest.cs
```csharp
namespace {Namespace}.Auth.DTOs;

public sealed record RefreshRequest(string RefreshToken);
```

### ChangePasswordRequest.cs
```csharp
namespace {Namespace}.Auth.DTOs;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
```

### ForgotPasswordRequest.cs
```csharp
namespace {Namespace}.Auth.DTOs;

public sealed record ForgotPasswordRequest(string Email);
```

### SelectTenantRequest.cs
```csharp
namespace {Namespace}.Auth.DTOs;

public sealed record SelectTenantRequest(string TenantId);
```

### TokenResponse.cs
```csharp
namespace {Namespace}.Auth.DTOs;

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);
```

### TenantListResponse.cs
```csharp
namespace {Namespace}.Auth.DTOs;

public sealed record TenantInfo(
    string TenantId,
    string Name,
    string Role,
    bool IsOwner);

public sealed record TenantListResponse(
    IReadOnlyList<TenantInfo> Tenants);
```

---

## AuthEndpoints.cs

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Nac.Identity.Entities;
using Nac.Identity.Services;
using {Namespace}.Auth.DTOs;

namespace {Namespace}.Auth.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        // Public endpoints
        group.MapPost("/login", Login);
        group.MapPost("/register", Register);
        group.MapPost("/refresh", RefreshTokens);
        group.MapPost("/forgot-password", ForgotPassword);

        // Authenticated endpoints
        group.MapGet("/tenants", GetTenants).RequireAuthorization();
        group.MapPost("/select-tenant", SelectTenant).RequireAuthorization();
        group.MapPost("/logout", Logout).RequireAuthorization();
        group.MapPost("/change-password", ChangePassword).RequireAuthorization();

        return app;
    }

    /// <summary>
    /// Step 1 of 2-step login: Authenticate user, return JWT WITHOUT tenant
    /// </summary>
    private static async Task<IResult> Login(
        LoginRequest request,
        UserManager<NacUser> userManager,
        IJwtTokenService tokenService)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Results.Unauthorized();

        if (!await userManager.CheckPasswordAsync(user, request.Password))
            return Results.Unauthorized();

        // Issue JWT WITHOUT tenant (step 1 of 2-step login)
        var tokens = await tokenService.GenerateTokensAsync(user, tenantId: null);

        return Results.Ok(new TokenResponse(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresAt));
    }

    /// <summary>
    /// Register new user
    /// </summary>
    private static async Task<IResult> Register(
        RegisterRequest request,
        UserManager<NacUser> userManager)
    {
        var user = new NacUser
        {
            Email = request.Email,
            UserName = request.Email,
            DisplayName = request.DisplayName ?? request.Email.Split('@')[0]
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return Results.BadRequest(new { errors });
        }

        return Results.Ok(new { userId = user.Id, message = "User created successfully" });
    }

    /// <summary>
    /// Get list of tenants user has access to
    /// </summary>
    private static async Task<IResult> GetTenants(
        ClaimsPrincipal user,
        ITenantRoleService tenantRoleService)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            return Results.Unauthorized();

        var userId = Guid.Parse(userIdClaim);
        var memberships = await tenantRoleService.GetUserMembershipsAsync(userId);

        var tenants = memberships.Select(m => new TenantInfo(
            m.TenantId,
            m.TenantId, // Use TenantId as name if TenantName not available
            m.TenantRole?.Name ?? "Member",
            m.IsOwner)).ToList();

        return Results.Ok(new TenantListResponse(tenants));
    }

    /// <summary>
    /// Step 2 of 2-step login: Select tenant, return JWT WITH tenant
    /// </summary>
    private static async Task<IResult> SelectTenant(
        SelectTenantRequest request,
        ClaimsPrincipal user,
        UserManager<NacUser> userManager,
        ITenantRoleService tenantRoleService,
        IJwtTokenService tokenService)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            return Results.Unauthorized();

        var userId = Guid.Parse(userIdClaim);
        var nacUser = await userManager.FindByIdAsync(userId.ToString());
        if (nacUser is null)
            return Results.Unauthorized();

        // Verify user has access to requested tenant
        var membership = await tenantRoleService.GetMembershipAsync(userId, request.TenantId);
        if (membership is null)
            return Results.Forbid();

        // Issue JWT WITH tenant (step 2)
        var tokens = await tokenService.GenerateTokensAsync(nacUser, tenantId: request.TenantId);

        return Results.Ok(new TokenResponse(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresAt));
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    private static async Task<IResult> RefreshTokens(
        RefreshRequest request,
        IJwtTokenService tokenService)
    {
        var result = await tokenService.RefreshTokensAsync(request.RefreshToken);

        if (result is null)
            return Results.Unauthorized();

        return Results.Ok(new TokenResponse(
            result.AccessToken,
            result.RefreshToken,
            result.ExpiresAt));
    }

    /// <summary>
    /// Logout - revoke refresh token
    /// </summary>
    private static async Task<IResult> Logout(
        ClaimsPrincipal user,
        IJwtTokenService tokenService,
        string? refreshToken = null)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            return Results.Unauthorized();

        var userId = Guid.Parse(userIdClaim);

        if (!string.IsNullOrEmpty(refreshToken))
        {
            await tokenService.RevokeTokenAsync(refreshToken);
        }
        else
        {
            await tokenService.RevokeAllTokensAsync(userId);
        }

        return Results.Ok(new { success = true });
    }

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    private static async Task<IResult> ChangePassword(
        ChangePasswordRequest request,
        ClaimsPrincipal user,
        UserManager<NacUser> userManager,
        IJwtTokenService tokenService)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            return Results.Unauthorized();

        var userId = Guid.Parse(userIdClaim);
        var nacUser = await userManager.FindByIdAsync(userId.ToString());
        if (nacUser is null)
            return Results.Unauthorized();

        var result = await userManager.ChangePasswordAsync(
            nacUser,
            request.CurrentPassword,
            request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return Results.BadRequest(new { errors });
        }

        // Revoke all tokens after password change
        await tokenService.RevokeAllTokensAsync(userId);

        return Results.Ok(new { success = true });
    }

    /// <summary>
    /// Trigger forgot password flow (email sending not implemented)
    /// </summary>
    private static async Task<IResult> ForgotPassword(
        ForgotPasswordRequest request,
        UserManager<NacUser> userManager)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        // Always return success to prevent email enumeration
        if (user is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            // TODO: Send email with reset link containing token
            // await emailService.SendPasswordResetAsync(user.Email, token);
        }

        return Results.Ok(new { message = "If email exists, reset link will be sent" });
    }
}
```

---

## csproj Template

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Nac.Identity" Version="*" />
  </ItemGroup>
</Project>
```

---

## Usage Notes

1. Replace `{Namespace}` with actual project namespace from `nac.json`
2. All DTOs use `sealed record` for immutability
3. `TokenResponse` includes `ExpiresAt` for client-side token refresh timing
4. Login returns JWT WITHOUT tenant - use `/select-tenant` for tenant-scoped JWT
5. `ForgotPassword` always returns success to prevent email enumeration
