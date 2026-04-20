# NAC Framework

Opinionated .NET 10 framework for DDD + multi-tenant + event-driven backends. Ships as a layered suite of NuGet packages — consumers integrate via NuGet, never by copying source.

## Highlights

- **L0 `Nac.Core`** — DDD primitives, Result, modularity, abstractions.
- **L1 `Nac.Cqrs`, `Nac.Caching`** — dispatcher, HybridCache wrapper.
- **L2 features** — `Nac.Persistence`, `Nac.EventBus`, `Nac.MultiTenancy` (+ `.Management`), `Nac.Identity` (+ `.Management`), `Nac.Observability`, `Nac.Jobs`, `Nac.Testing`.
- **L3 composition** — `Nac.WebApi` (module loader, middleware pipeline, RFC 9457 errors, OpenAPI, versioning).

## Identity Quickstart (v3 Pattern A)

```csharp
// Program.cs
services.AddNacIdentity<AppDbContext>(opts => {
    opts.JwtIssuer     = "my-app";
    opts.JwtAudience   = "nac-staff";
    opts.JwtSigningKey = builder.Configuration["Jwt:Key"]!;
});
services.AddNacIdentityManagement();    // /api/identity/* admin HTTP
// Role templates (owner/admin/member/guest) seeded automatically by AddNacIdentity<T>.
// Add your own via: services.AddSingleton<IRoleTemplateProvider, MyTemplates>();

var app = builder.Build();
app.MapNacAuthEndpoints();             // /auth/login, /switch-tenant, /me, ...
app.UseNacAuthGate();                  // TenantRequiredGateMiddleware
app.MapControllers();
```

Login flow:

1. `POST /auth/login` → tenantless JWT + `memberships[]`.
2. Client picks a tenant → `POST /auth/switch-tenant` → tenant-scoped JWT (`tenant_id`, `role_ids`).
3. Permission checks run at request time via `IPermissionChecker` (cache-backed, instant revoke).

Full spec: **[docs/identity-and-rbac.md](docs/identity-and-rbac.md)** — includes the Customer Identity Pattern Guide for building your own customer stack alongside the staff stack.

## Documentation

- [System Architecture](docs/system-architecture.md)
- [Codebase Summary](docs/codebase-summary.md)
- [Identity & RBAC](docs/identity-and-rbac.md)
- [Changelog](docs/project-changelog.md)
- [Roadmap](docs/project-roadmap.md)
- [Code Standards](docs/code-standards.md)

## License

MIT.
