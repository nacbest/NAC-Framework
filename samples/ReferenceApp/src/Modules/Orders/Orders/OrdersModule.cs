using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nac.Core.Abstractions.Permissions;
using Nac.Core.DataSeeding;
using Nac.Core.Modularity;
using Nac.Cqrs;
using Nac.Cqrs.Extensions;
using Nac.EventBus;
using Nac.Persistence;
using Nac.Persistence.Extensions;
using Orders.DataSeeding;
using Orders.Features.CreateOrder;
using Orders.Infrastructure;
using ReferenceApp.SharedKernel.Infrastructure;
using Orders.Permissions;

namespace Orders;

/// <summary>
/// NAC module descriptor for the Orders bounded context.
/// Registers: DbContext (with outbox + audit), CQRS handlers, validator,
/// permission provider, and data seeder.
/// </summary>
[DependsOn(
    typeof(NacPersistenceModule),
    typeof(NacCqrsModule),
    typeof(NacEventBusModule))]
public sealed class OrdersModule : NacModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = context.Configuration;

        // ── Persistence ────────────────────────────────────────────────────────
        // AddNacPersistence<T> registers: TContext, NacDbContext alias, IUnitOfWork,
        // open-generic IRepository<>/IReadRepository<>, and enabled interceptors.
        services.AddNacPersistence<OrdersDbContext>(opts =>
            opts
                .UseDbContext(builder => builder.UseNpgsql(
                    configuration.GetConnectionString("Default"),
                    npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "orders")))
                .EnableAuditInterceptor()
                .EnableOutbox());

        // ── Repository ─────────────────────────────────────────────────────────
        services.AddScoped<IOrderRepository, OrderRepository>();

        // ── CQRS — assembly scan registers all ICommandHandler / IQueryHandler ──
        // Also wires ValidationBehavior (FluentValidation) + TransactionBehavior.
        services.AddNacCqrs(opts =>
            opts.RegisterHandlersFromAssembly(typeof(OrdersModule).Assembly)
                .AddValidationBehavior()
                .AddTransactionBehavior());

        // ── FluentValidation ───────────────────────────────────────────────────
        services.AddScoped<IValidator<CreateOrderCommand>, CreateOrderValidator>();

        // ── Permissions ────────────────────────────────────────────────────────
        services.AddSingleton<IPermissionDefinitionProvider, OrderPermissionProvider>();

        // ── Data seeding ───────────────────────────────────────────────────────
        services.AddScoped<IDataSeeder, OrdersDataSeeder>();

        // ── Migration runner (host resolves IEnumerable<IMigrationRunner> at startup) ──
        services.AddScoped<IMigrationRunner, OrdersMigrationRunner>();
    }
}
