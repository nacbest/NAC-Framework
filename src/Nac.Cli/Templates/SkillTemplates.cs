namespace Nac.Cli.Templates;

/// <summary>
/// Embedded Claude skill templates for nac-identity.
/// </summary>
internal static class SkillTemplates
{
    public static string NacIdentitySkillMd() => """
        ---
        name: nac-identity
        description: "Implement Nac.Identity in NAC-based projects with migrations and auth endpoints"
        argument-hint: "[--skip-migration]"
        ---

        # Nac.Identity Implementation Skill

        Guided workflow to add authentication and multi-tenancy to NAC-based projects.

        ## Workflow

        ```mermaid
        flowchart TD
            A[Validate Project] --> B{Valid?}
            B -->|No| C[Report & Stop]
            B -->|Yes| D[Generate Auth Module]
            D --> E[Wire Host]
            E --> F{--skip-migration?}
            F -->|Yes| J
            F -->|No| G[Create Migration]
            G --> H{User Confirms?}
            H -->|Yes| I[Apply Migration]
            H -->|No| J[Skip Migration]
            I --> K[Verify Build]
            J --> K
            K --> L[Report Summary]
        ```

        ## Prerequisites

        - NAC project with `nac.json`
        - PostgreSQL database configured
        - Connection string in `appsettings.json`

        ---

        ## Step 1: Validate Project

        1. Read `nac.json` - extract `namespace` and `modules` list
        2. Check `src/{Namespace}.Host/` exists
        3. Verify `appsettings.json` has `ConnectionStrings` section
        4. Check if `Nac.Identity` package referenced

        **If validation fails:** Report missing items, stop workflow.

        ```bash
        # Quick validation commands
        cat nac.json | jq '.namespace, .modules'
        ls src/*/appsettings.json
        grep -r "Nac.Identity" src/*/*.csproj
        ```

        ---

        ## Step 2: Generate Auth Module

        Create module structure:

        ```
        src/Modules/{Namespace}.Auth/
        ├── {Namespace}.Auth.csproj
        ├── DTOs/
        │   ├── LoginRequest.cs
        │   ├── RegisterRequest.cs
        │   ├── RefreshRequest.cs
        │   ├── ChangePasswordRequest.cs
        │   ├── ForgotPasswordRequest.cs
        │   ├── SelectTenantRequest.cs
        │   ├── TokenResponse.cs
        │   └── TenantListResponse.cs
        └── Endpoints/
            └── AuthEndpoints.cs
        ```

        **Load:** `references/auth-endpoints.md` for complete code patterns.

        ### Generation Steps

        1. Create directories: `mkdir -p src/Modules/{Namespace}.Auth/{DTOs,Endpoints}`
        2. Create `.csproj` with `Nac.Identity` reference
        3. Create all 8 DTO files
        4. Create `AuthEndpoints.cs` with all endpoint implementations
        5. Update `nac.json` to register module

        ---

        ## Step 3: Wire Host Integration

        ### Update Program.cs

        Add these lines in order:

        ```csharp
        // After builder creation
        using Nac.Identity.Extensions;

        // In service configuration
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddNacIdentity(builder.Configuration, db => db.UseNpgsql(connectionString));

        // After app creation, before UseRouting
        app.UseNacIdentity(seedRoles: true);

        // Map auth endpoints
        app.MapAuthEndpoints();
        ```

        ### Update appsettings.json

        Add `NacIdentity` section:

        ```json
        {
          "NacIdentity": {
            "SigningKey": "your-signing-key-min-32-chars-here-change-in-production",
            "Issuer": "{Namespace}",
            "Audience": "{Namespace}",
            "AccessTokenExpiry": "00:15:00",
            "RefreshTokenExpiry": "7.00:00:00"
          }
        }
        ```

        ### Add Package Reference (if missing)

        ```xml
        <PackageReference Include="Nac.Identity" Version="*" />
        <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="*" />
        ```

        ---

        ## Step 4: Create Migration

        <HARD-GATE>
        MUST use AskUserQuestion before applying migration.
        NEVER skip confirmation.
        NEVER auto-apply migrations.
        </HARD-GATE>

        **If `--skip-migration` flag provided:** Skip to Step 5.

        **Load:** `references/migration-safety.md` for confirmation protocol.

        ### Migration Commands

        ```bash
        # Create migration
        dotnet ef migrations add InitialIdentity \
          -p src/Nac.Identity \
          -s src/{Namespace}.Host

        # Generate SQL preview
        dotnet ef migrations script \
          -p src/Nac.Identity \
          -s src/{Namespace}.Host \
          --idempotent
        ```

        ### Confirmation Flow

        1. Generate SQL preview
        2. Show tables that will be created:
           - `AspNetUsers`
           - `AspNetRoles`
           - `AspNetUserRoles`
           - `TenantRoles`
           - `TenantMemberships`
           - `RefreshTokens`
        3. Use `AskUserQuestion` with options:
           - "Yes, apply migration"
           - "No, skip for now"

        ### Apply or Skip

        **If confirmed:**
        ```bash
        dotnet ef database update \
          -p src/Nac.Identity \
          -s src/{Namespace}.Host
        ```

        **If skipped:**
        Inform user: "Migration files created. Run manually when ready:
        `dotnet ef database update -p src/Nac.Identity -s src/{Namespace}.Host`"

        ---

        ## Step 5: Verify

        1. Run `dotnet build src/{Namespace}.Host`
        2. Report any errors
        3. Summarize created items:
           - Auth module with 8 endpoints
           - Host integration
           - Database tables (if migration applied)

        ### Success Output

        ```
        ✓ Nac.Identity implemented successfully

        Created:
        - src/Modules/{Namespace}.Auth/ (8 DTOs, AuthEndpoints.cs)
        - Host integration (Program.cs, appsettings.json)
        - Database tables: 6 tables (if migration applied)

        Endpoints available:
        - POST /auth/login
        - POST /auth/register
        - GET  /auth/tenants
        - POST /auth/select-tenant
        - POST /auth/refresh
        - POST /auth/logout
        - POST /auth/change-password
        - POST /auth/forgot-password

        Next steps:
        1. Start host: dotnet run --project src/{Namespace}.Host
        2. Test login: curl -X POST http://localhost:5000/auth/register -H "Content-Type: application/json" -d '{"email":"test@example.com","password":"Test123!"}'
        ```

        ---

        ## Multi-Tenancy Options

        **Load:** `references/tenant-flows.md` for detailed patterns.

        | Flow | Use Case | Configuration |
        |------|----------|---------------|
        | 2-Step Login | Admin panels | Default (no extra config) |
        | Domain-Based | Public sites | Add `AddNacMultiTenancy()` |
        | Hybrid | Both | Route-based configuration |

        ---

        ## Arguments

        | Argument | Description |
        |----------|-------------|
        | `--skip-migration` | Generate code only, skip database migration step |

        ---

        ## Error Recovery

        | Error | Resolution |
        |-------|------------|
        | `nac.json` not found | Initialize with `nac new` first |
        | Connection string missing | Add to `appsettings.json` |
        | EF tools not installed | `dotnet tool install -g dotnet-ef` |
        | Migration fails | Check `references/migration-safety.md` for rollback |
        """;

    public static string AuthEndpointsMd() => """
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
                    m.TenantId,
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
        """;

    public static string TenantFlowsMd() => """
        # Tenant Authentication Flows

        Multi-tenancy authentication patterns: 2-step login (admin) and domain-based (public).

        ## Flow Comparison

        | Aspect | Admin (2-Step Login) | Public (Domain-Based) |
        |--------|---------------------|----------------------|
        | **Use Case** | Admin panel, back-office | Customer-facing websites |
        | **Tenant Selection** | User chooses after login | Auto-detected from domain |
        | **JWT Flow** | No tenant → With tenant | Tenant from start |
        | **UX** | Explicit tenant picker | Seamless, transparent |

        ---

        ## Admin Flow: 2-Step Login

        ```
        ┌─────────────────────────────────────────────────────────────┐
        │                    2-Step Login Flow                         │
        ├─────────────────────────────────────────────────────────────┤
        │                                                             │
        │  Step 1: Authenticate User                                  │
        │  ┌─────────────────┐    ┌─────────────────┐                │
        │  │ POST /auth/login│───►│ JWT (no tenant) │                │
        │  │ {email, pass}   │    │ user_id only    │                │
        │  └─────────────────┘    └────────┬────────┘                │
        │                                  │                          │
        │  Step 2: List Available Tenants  │                          │
        │  ┌─────────────────┐    ┌────────▼────────┐                │
        │  │ GET /auth/tenants│◄──│ Bearer: JWT     │                │
        │  └────────┬────────┘    └─────────────────┘                │
        │           │                                                 │
        │           ▼                                                 │
        │  ┌─────────────────────────────────────────┐               │
        │  │ [{tenantId, name, role, isOwner}, ...]   │               │
        │  └────────┬────────────────────────────────┘               │
        │           │                                                 │
        │  Step 3: Select Tenant                                      │
        │  ┌────────▼────────┐    ┌─────────────────┐                │
        │  │POST /auth/select│───►│ JWT (with tenant)│                │
        │  │  {tenantId}     │    │ tenant_id claim  │                │
        │  └─────────────────┘    └─────────────────┘                │
        │                                                             │
        └─────────────────────────────────────────────────────────────┘
        ```

        ### Frontend Implementation (TypeScript)

        ```typescript
        interface TokenResponse {
          accessToken: string;
          refreshToken: string;
          expiresAt: string;
        }

        interface TenantInfo {
          tenantId: string;
          name: string;
          role: string;
          isOwner: boolean;
        }

        async function adminLogin(email: string, password: string): Promise<TokenResponse> {
          // Step 1: Get initial JWT (no tenant)
          const loginResponse = await fetch('/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
          });

          if (!loginResponse.ok) throw new Error('Login failed');
          const { accessToken } = await loginResponse.json();

          // Step 2: Fetch available tenants
          const tenantsResponse = await fetch('/auth/tenants', {
            headers: { Authorization: `Bearer ${accessToken}` }
          });

          const { tenants }: { tenants: TenantInfo[] } = await tenantsResponse.json();

          // Step 3: User selects tenant (show UI picker)
          const selectedTenantId = await showTenantPicker(tenants);

          // Step 4: Get tenant-scoped JWT
          const selectResponse = await fetch('/auth/select-tenant', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              Authorization: `Bearer ${accessToken}`
            },
            body: JSON.stringify({ tenantId: selectedTenantId })
          });

          return await selectResponse.json();
        }
        ```

        ---

        ## Public Flow: Domain-Based

        ```
        ┌─────────────────────────────────────────────────────────────┐
        │                  Domain-Based Tenant Flow                    │
        ├─────────────────────────────────────────────────────────────┤
        │                                                             │
        │  Request: https://acme.myapp.com/auth/login                 │
        │                    ▼                                        │
        │  ┌─────────────────────────────────────────┐               │
        │  │  TenantResolutionMiddleware             │               │
        │  │  - Extract: acme from subdomain         │               │
        │  │  - Set: ITenantContext.TenantId = acme  │               │
        │  └────────────────┬────────────────────────┘               │
        │                   │                                         │
        │                   ▼                                         │
        │  ┌─────────────────────────────────────────┐               │
        │  │  POST /auth/login                       │               │
        │  │  - Read tenant from ITenantContext      │               │
        │  │  - Verify user belongs to tenant        │               │
        │  │  - Issue JWT WITH tenant_id = acme      │               │
        │  └─────────────────────────────────────────┘               │
        │                                                             │
        └─────────────────────────────────────────────────────────────┘
        ```

        ### Tenant Resolution Strategies

        Configure in `Program.cs`:

        ```csharp
        // Strategy 1: Subdomain (acme.app.com)
        services.AddNacMultiTenancy(opts =>
        {
            opts.UseSubdomainResolution();
        });

        // Strategy 2: Path (app.com/acme/...)
        services.AddNacMultiTenancy(opts =>
        {
            opts.UsePathResolution();
        });

        // Strategy 3: Header (X-Tenant-Id: acme)
        services.AddNacMultiTenancy(opts =>
        {
            opts.UseHeaderResolution("X-Tenant-Id");
        });

        // Strategy 4: Custom domain mapping
        services.AddNacMultiTenancy(opts =>
        {
            opts.UseCustomResolution(async (context, tenantStore) =>
            {
                var host = context.Request.Host.Host;
                var tenant = await tenantStore.GetByDomainAsync(host);
                return tenant?.Id;
            });
        });
        ```

        ---

        ## Hybrid Approach

        For apps needing both flows (admin + public):

        ```csharp
        // Program.cs
        var app = builder.Build();

        // Admin routes: 2-step login (no tenant middleware)
        app.MapGroup("/admin/auth")
           .MapAuthEndpoints()
           .WithTags("Admin Auth");

        // Public routes: Domain-based (tenant middleware active)
        app.MapGroup("/api/auth")
           .RequireTenantContext()
           .MapPublicAuthEndpoints()
           .WithTags("Public Auth");
        ```

        ---

        ## JWT Claims Comparison

        ### 2-Step Login (Step 1 - No Tenant)
        ```json
        {
          "sub": "user-guid",
          "email": "user@example.com",
          "name": "John Doe",
          "iat": 1234567890,
          "exp": 1234568790
        }
        ```

        ### 2-Step Login (Step 2 - With Tenant)
        ```json
        {
          "sub": "user-guid",
          "email": "user@example.com",
          "name": "John Doe",
          "tenant_id": "acme",
          "role": "Admin",
          "iat": 1234567890,
          "exp": 1234568790
        }
        ```

        ---

        ## Security Considerations

        1. **Tenant Isolation**: Always verify user has membership in requested tenant
        2. **Token Scope**: JWT without tenant grants limited access (user profile only)
        3. **Domain Validation**: Validate custom domains against whitelist
        4. **Cross-Tenant Access**: Never allow JWT from one tenant to access another
        """;

    public static string MigrationSafetyMd() => """
        # Migration Safety Protocol

        <HARD-GATE>
        Database migrations MUST be confirmed by user before applying.
        NEVER skip this confirmation step.
        NEVER auto-apply migrations.
        </HARD-GATE>

        ---

        ## Migration Workflow

        ```
        ┌─────────────────────────────────────────────────────────────┐
        │                Migration Safety Protocol                     │
        ├─────────────────────────────────────────────────────────────┤
        │                                                             │
        │  Step 1: Create Migration Files                             │
        │  ┌─────────────────────────────────────────────────────┐   │
        │  │ dotnet ef migrations add InitialIdentity            │   │
        │  │   -p src/Nac.Identity                               │   │
        │  │   -s src/{Namespace}.Host                           │   │
        │  └─────────────────────────────────────────────────────┘   │
        │                         │                                   │
        │                         ▼                                   │
        │  Step 2: Generate SQL Preview                               │
        │  ┌─────────────────────────────────────────────────────┐   │
        │  │ dotnet ef migrations script                         │   │
        │  │   -p src/Nac.Identity                               │   │
        │  │   -s src/{Namespace}.Host                           │   │
        │  │   --idempotent                                      │   │
        │  └─────────────────────────────────────────────────────┘   │
        │                         │                                   │
        │                         ▼                                   │
        │  Step 3: Show Confirmation Dialog                           │
        │  ┌─────────────────────────────────────────────────────┐   │
        │  │ AskUserQuestion with SQL preview                    │   │
        │  │ Options: [Apply] [Skip]                             │   │
        │  └─────────────────────────────────────────────────────┘   │
        │                         │                                   │
        │           ┌─────────────┴─────────────┐                    │
        │           ▼                           ▼                    │
        │  ┌─────────────┐            ┌─────────────────┐           │
        │  │ User: Apply │            │ User: Skip      │           │
        │  └──────┬──────┘            └────────┬────────┘           │
        │         │                            │                     │
        │         ▼                            ▼                     │
        │  ┌──────────────────┐     ┌────────────────────────┐      │
        │  │ database update  │     │ Inform: Run manually   │      │
        │  │ Report success   │     │ dotnet ef db update    │      │
        │  └──────────────────┘     └────────────────────────┘      │
        │                                                             │
        └─────────────────────────────────────────────────────────────┘
        ```

        ---

        ## AskUserQuestion Template

        Use this exact format for migration confirmation:

        ```json
        {
          "questions": [{
            "question": "Apply database migration? This will create Identity tables.",
            "header": "Database Migration",
            "options": [
              {
                "label": "Yes, apply migration",
                "description": "Creates: AspNetUsers, AspNetRoles, TenantRoles, TenantMemberships, RefreshTokens"
              },
              {
                "label": "No, skip for now",
                "description": "Migration files created. Run manually: dotnet ef database update"
              }
            ],
            "multiSelect": false
          }]
        }
        ```

        ---

        ## SQL Preview Format

        When showing SQL preview to user, include:

        1. **Tables being created** (list names)
        2. **Key columns** (ID types, important fields)
        3. **Indexes and constraints**
        4. **Truncate if > 50 lines** (show summary)

        ### Example Preview

        ```sql
        -- Identity Tables to be created:
        -- 1. AspNetUsers      (Identity users - UUID primary key)
        -- 2. AspNetRoles      (System roles)
        -- 3. AspNetUserRoles  (User-Role mapping)
        -- 4. TenantRoles      (Tenant-scoped roles with permissions)
        -- 5. TenantMemberships (User-Tenant-Role links)
        -- 6. RefreshTokens    (JWT refresh tokens)

        CREATE TABLE "AspNetUsers" (
            "Id" uuid NOT NULL,
            "DisplayName" varchar(256),
            "Email" varchar(256),
            "PasswordHash" text,
            CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id")
        );

        CREATE TABLE "TenantRoles" (
            "Id" uuid NOT NULL,
            "TenantId" varchar(64) NOT NULL,
            "Name" varchar(64) NOT NULL,
            "Permissions" jsonb NOT NULL DEFAULT '[]',
            CONSTRAINT "PK_TenantRoles" PRIMARY KEY ("Id")
        );

        -- ... (showing 2 of 6 tables)
        -- Full script: ~150 lines
        ```

        ---

        ## Error Handling

        ### Migration Creation Fails

        ```markdown
        If `dotnet ef migrations add` fails:

        1. Check EF tools installed:
           dotnet tool list -g | grep dotnet-ef

        2. Install if missing:
           dotnet tool install -g dotnet-ef

        3. Check DbContext configuration

        4. Report error message to user

        5. DO NOT proceed to apply step
        ```

        ### Database Update Fails

        ```markdown
        If `dotnet ef database update` fails:

        1. Report full error message

        2. Common issues:
           - Connection string invalid
           - Database doesn't exist
           - Permission denied
           - Table already exists

        3. Suggest rollback if needed
        ```

        ---

        ## Rollback Commands

        Always inform user about rollback options:

        ### Remove Last Migration (if not applied)

        ```bash
        dotnet ef migrations remove \
          -p src/Nac.Identity \
          -s src/{Namespace}.Host
        ```

        ### Revert to Previous Migration

        ```bash
        dotnet ef database update {PreviousMigrationName} \
          -p src/Nac.Identity \
          -s src/{Namespace}.Host
        ```

        ### Drop All Identity Tables (DESTRUCTIVE)

        ```sql
        -- WARNING: This deletes all identity data!
        DROP TABLE IF EXISTS "RefreshTokens";
        DROP TABLE IF EXISTS "TenantMemberships";
        DROP TABLE IF EXISTS "TenantRoles";
        DROP TABLE IF EXISTS "AspNetUserRoles";
        DROP TABLE IF EXISTS "AspNetRoles";
        DROP TABLE IF EXISTS "AspNetUsers";
        ```
        """;
}
