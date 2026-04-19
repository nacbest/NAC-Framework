using Billing;
using Microsoft.EntityFrameworkCore;
using Orders.Contracts.IntegrationEvents;
using Nac.Caching.Extensions;
using Nac.Core.Abstractions;
using Nac.Core.DataSeeding;
using Nac.Core.Extensions;
using Nac.Cqrs.Extensions;
using Nac.EventBus.Extensions;
using Nac.Identity.Extensions;
using Nac.MultiTenancy.Abstractions;
using Nac.MultiTenancy.Context;
using Nac.MultiTenancy.Extensions;
using Nac.MultiTenancy.Resolution;
using Nac.Persistence.Extensions;
using Nac.WebApi.Extensions;
using Orders;
using ReferenceApp.Host;
using ReferenceApp.SharedKernel.Authorization;
using ReferenceApp.SharedKernel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── 0. Infrastructure primitives ─────────────────────────────────────────────
// IDateTimeProvider: used by AuditableEntityInterceptor + OutboxInterceptor.
// Framework defines the interface but provides no default implementation.
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

// HybridCache: required by NacCache. AddNacCaching() registers NacCache but
// does not call AddHybridCache() — consumer must register it first.
builder.Services.AddHybridCache();

// ── 1. WebApi options FIRST (framework enforces: must precede AddNacApplication) ─
builder.Services.AddNacWebApi(opt =>
{
    opt.EnableOpenApi = true;
    opt.EnableCors = true;
    opt.EnableHealthChecks = true;
});

// ── 2. Persistence: AppDbContext (host-owned identity store) ──────────────────
// AppDbContext inherits NacIdentityDbContext → NacDbContext.
// Interceptors (audit, soft-delete) deliberately omitted: identity tables managed
// by ASP.NET Core Identity; no domain events or outbox on the identity context.
builder.Services.AddNacPersistence<AppDbContext>(opts =>
    opts.UseDbContext(b => b.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "identity"))));

// ── 3. Multi-tenancy: resolve tenant from X-Tenant-Id header ─────────────────
builder.Services.AddNacMultiTenancy(opt =>
{
    opt.DefaultTenantId = builder.Configuration["MultiTenancy:DefaultTenantId"] ?? "default";
    opt.Strategies.Add(typeof(HeaderTenantStrategy));
});

// Register in-memory tenant store with the default tenant.
// TenantResolutionMiddleware requires ITenantStore to look up TenantInfo by ID.
// Production: replace with EF-backed or config-backed store.
builder.Services.AddSingleton<ITenantStore>(new InMemoryTenantStore(
[
    new TenantInfo { Id = "default", Name = "Default Tenant", IsActive = true }
]));

// ── 4. CQRS pipeline (host-level behaviors; modules register their own handlers) ─
builder.Services.AddNacCqrs(c =>
    c.AddLoggingBehavior()
     .AddValidationBehavior()
     .AddCachingBehavior()
     .AddTransactionBehavior());

// ── 5. EventBus: in-memory transport, scan both module assemblies ─────────────
// Register event bus with all three relevant assemblies:
//   Orders.dll          — no handlers, no events (but scanned for completeness)
//   Billing.dll         — contains OrderCreatedEventHandler
//   Orders.Contracts.dll — contains OrderCreatedEvent (IIntegrationEvent implementation)
//                          REQUIRED for OutboxEventTypeRegistry to resolve event type names
//                          when OutboxWorker deserializes outbox rows.
// NOTE: NacEventBusModule and BillingModule.ConfigureServices also call AddNacEventBus
// (UseInMemory=true by default). The last AddNacEventBus call here wins for:
//   - IEventPublisher (last InMemoryEventBus instance written to the final Channel)
//   - OutboxEventTypeRegistry (last registry, must include Orders.Contracts)
//   - FrozenDictionary<Type,FrozenSet<Type>> handler registry (last wins)
builder.Services.AddNacEventBus(opt =>
    opt.RegisterHandlersFromAssembly(typeof(OrdersModule).Assembly)
       .RegisterHandlersFromAssembly(typeof(BillingModule).Assembly)
       .RegisterHandlersFromAssembly(typeof(OrderCreatedEvent).Assembly)
       .UseInMemoryTransport());

// ── 6. Identity + JWT (must be called after AddNacPersistence<AppDbContext>) ──
builder.Services.AddNacIdentity<AppDbContext>(opt =>
{
    opt.Jwt.SecretKey         = builder.Configuration["Jwt:SecretKey"]!;
    opt.Jwt.Issuer            = builder.Configuration["Jwt:Issuer"]!;
    opt.Jwt.Audience          = builder.Configuration["Jwt:Audience"]!;
    opt.Jwt.ExpirationMinutes = int.Parse(
        builder.Configuration["Jwt:ExpirationMinutes"] ?? "60");
});

// ── 7. Caching ────────────────────────────────────────────────────────────────
builder.Services.AddNacCaching();

// ── 8. Permission policy provider (PermissionAuthorizationHandler already in AddNacIdentity)
builder.Services.AddNacPermissionPolicies();

// ── 9. NAC module system — discovers all [DependsOn] modules transitively ─────
// MUST be called AFTER AddNacWebApi (framework guard throws if ordering is wrong).
builder.Services.AddNacApplication<AppRootModule>(builder.Configuration);

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// Middleware pipeline: exception handler → HTTPS → routing → multi-tenancy →
// authentication → authorization → controllers → OpenAPI → health checks.
// Ordering is managed inside UseNacApplication per NacWebApiOptions flags.
app.UseNacApplication();

// ── Dev-only startup tasks: migrate + seed ────────────────────────────────────
// Production: use a separate migration job / Flyway / EF bundle.
// README: do NOT run with multiple replicas in dev without external migration lock.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;

    // 1. Identity schema first (no cross-schema FK dependencies, but convention).
    await sp.GetRequiredService<AppDbContext>().Database.MigrateAsync();

    // 2. Module contexts — via IMigrationRunner (keeps module DbContexts internal).
    //    Registration order in DI matches module ConfigureServices call order:
    //    OrdersModule first, BillingModule second (matches [DependsOn] sequence).
    var runners = sp.GetServices<IMigrationRunner>();
    foreach (var runner in runners)
        await runner.RunAsync();

    // 3. Data seeders: AdminSeeder (Host) + OrdersDataSeeder.
    var seedContext = new DataSeedContext(sp);
    foreach (var seeder in sp.GetServices<IDataSeeder>())
        await seeder.SeedAsync(seedContext);
}

app.Run();

// Exposes Program to WebApplicationFactory for Phase 06 integration tests.
public partial class Program { }
