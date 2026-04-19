using Billing.Features.GetInvoiceById;
using Billing.Infrastructure;
using Billing.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nac.Core.Abstractions.Permissions;
using Nac.Core.Modularity;
using Nac.Cqrs;
using Nac.Cqrs.Extensions;
using Nac.EventBus;
using Nac.EventBus.Extensions;
using Nac.Persistence;
using Nac.Persistence.Extensions;
using Orders.Contracts.IntegrationEvents;
using ReferenceApp.SharedKernel.Infrastructure;

namespace Billing;

/// <summary>
/// NAC module descriptor for the Billing bounded context.
/// Listens to <see cref="OrderCreatedEvent"/> from Orders module (via outbox/in-memory bus)
/// and creates Customer + Invoice records.
///
/// NOTE: Does NOT DependsOn OrdersModule — only references Orders.Contracts assembly.
/// Coupling is event-driven, not direct module dependency.
///
/// EventBus assembly registration: BillingModule calls AddNacEventBus with its own assembly
/// (contains OrderCreatedEventHandler) PLUS Orders.Contracts assembly (contains
/// OrderCreatedEvent — required by OutboxEventTypeRegistry for deserialization).
/// In .NET DI, last AddSingleton(instance) wins for GetRequiredService, so this call
/// overrides the empty registry registered by NacEventBusModule.
/// </summary>
[DependsOn(
    typeof(NacPersistenceModule),
    typeof(NacCqrsModule),
    typeof(NacEventBusModule))]
public sealed class BillingModule : NacModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = context.Configuration;

        // ── Persistence ────────────────────────────────────────────────────────
        // NOTE: Outbox intentionally NOT enabled here.
        // Framework limitation: AddNacPersistence<TContext> registers NacDbContext alias
        // via "last-registration-wins" → only the last caller's OutboxWorker polls.
        // Since Orders publishes OrderCreatedEvent, outbox is enabled on OrdersDbContext only.
        // Re-enable here when framework supports generic OutboxWorker<TContext>.
        services.AddNacPersistence<BillingDbContext>(opts =>
            opts
                .UseDbContext(builder => builder.UseNpgsql(
                    configuration.GetConnectionString("Default"),
                    npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "billing"))));

        // ── CQRS — assembly scan registers all IQueryHandler implementations ──
        services.AddNacCqrs(opts =>
            opts.RegisterHandlersFromAssembly(typeof(BillingModule).Assembly));

        // ── EventBus — scan Billing assembly for IEventHandler<T> implementations ──
        // Also includes Orders.Contracts assembly so OutboxEventTypeRegistry can resolve
        // OrderCreatedEvent type name during deserialization from outbox payload.
        services.AddNacEventBus(opts =>
            opts
                .RegisterHandlersFromAssembly(typeof(BillingModule).Assembly)
                .RegisterHandlersFromAssembly(typeof(OrderCreatedEvent).Assembly));

        // ── Permissions ────────────────────────────────────────────────────────
        services.AddSingleton<IPermissionDefinitionProvider, BillingPermissionProvider>();

        // ── Migration runner (host resolves IEnumerable<IMigrationRunner> at startup) ──
        services.AddScoped<IMigrationRunner, BillingMigrationRunner>();
    }
}
