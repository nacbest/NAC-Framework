using Billing;
using Microsoft.Extensions.DependencyInjection;
using Nac.Core.DataSeeding;
using Nac.Core.Modularity;
using Nac.Identity;
using Nac.MultiTenancy;
using Nac.WebApi;
using Orders;
using ReferenceApp.Host.Seeding;

namespace ReferenceApp.Host;

/// <summary>
/// Composition root module. Declares all module dependencies.
/// Phase 02: initial skeleton with NacWebApiModule.
/// Phase 03: added OrdersModule.
/// Phase 04: added BillingModule.
/// Phase 05: added NacIdentityModule + NacMultiTenancyModule + AdminSeeder.
/// </summary>
[DependsOn(typeof(NacWebApiModule))]
[DependsOn(typeof(NacIdentityModule))]
[DependsOn(typeof(NacMultiTenancyModule))]
// Order matters: Billing registers BEFORE Orders so OrdersDbContext is the LAST
// AddNacPersistence call. `NacDbContext` alias = last-registration-wins, so
// OutboxWorker polls `orders.__outbox_events` (Orders is the only publisher).
// Remove this note when framework exposes generic OutboxWorker<TContext>.
[DependsOn(typeof(BillingModule))]
[DependsOn(typeof(OrdersModule))]
public sealed class AppRootModule : NacModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Admin seeder: creates "admin" role + grants all module permissions + seeds admin user.
        context.Services.AddScoped<IDataSeeder, AdminSeeder>();
    }
}
