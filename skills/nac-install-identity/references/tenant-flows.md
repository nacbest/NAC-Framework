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

// Example tenant picker UI
async function showTenantPicker(tenants: TenantInfo[]): Promise<string> {
  return new Promise((resolve) => {
    // Show modal with tenant list
    // User clicks on a tenant
    // resolve(selectedTenantId)
  });
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
        
        // Check custom domain mapping
        var tenant = await tenantStore.GetByDomainAsync(host);
        if (tenant is not null)
            return tenant.Id;
        
        // Fallback to subdomain
        var parts = host.Split('.');
        return parts.Length > 2 ? parts[0] : null;
    });
});
```

### Public Login Endpoint (Modified)

```csharp
private static async Task<IResult> PublicLogin(
    LoginRequest request,
    ITenantContext tenantContext,  // Injected by middleware
    UserManager<NacUser> userManager,
    ITenantRoleService tenantRoleService,
    IJwtTokenService tokenService)
{
    // Tenant already resolved from domain
    var tenantId = tenantContext.TenantId;
    if (string.IsNullOrEmpty(tenantId))
        return Results.BadRequest(new { error = "Tenant not resolved from domain" });

    var user = await userManager.FindByEmailAsync(request.Email);
    if (user is null)
        return Results.Unauthorized();

    if (!await userManager.CheckPasswordAsync(user, request.Password))
        return Results.Unauthorized();

    // Verify user belongs to this tenant
    var membership = await tenantRoleService.GetMembershipAsync(user.Id, tenantId);
    if (membership is null)
        return Results.Forbid(); // User exists but not in this tenant

    // Issue JWT WITH tenant (single step)
    var tokens = await tokenService.GenerateTokensAsync(user, tenantId);
    
    return Results.Ok(new TokenResponse(
        tokens.AccessToken,
        tokens.RefreshToken,
        tokens.ExpiresAt));
}
```

---

## Hybrid Approach

For apps needing both flows (admin + public):

```csharp
// Program.cs
var app = builder.Build();

// Admin routes: 2-step login (no tenant middleware)
app.MapGroup("/admin/auth")
   .MapAuthEndpoints()  // Original 2-step flow
   .WithTags("Admin Auth");

// Public routes: Domain-based (tenant middleware active)
app.MapGroup("/api/auth")
   .RequireTenantContext()  // Middleware enforces tenant
   .MapPublicAuthEndpoints()
   .WithTags("Public Auth");
```

### Route Configuration

| Route Group | Tenant Resolution | Login Flow |
|-------------|-------------------|------------|
| `/admin/auth/*` | None (user selects) | 2-step |
| `/api/auth/*` | Domain/subdomain | Single step |

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

### Domain-Based (Single Step)
```json
{
  "sub": "user-guid",
  "email": "user@example.com",
  "name": "John Doe",
  "tenant_id": "acme",
  "role": "Member",
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
