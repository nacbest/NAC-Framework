using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Orders.Infrastructure;

/// <summary>
/// Design-time factory used exclusively by <c>dotnet ef</c> tooling (migrations, scaffolding).
/// Never resolved at runtime — EF Core discovers it by convention during design-time operations.
/// Uses a placeholder connection string; real connection string comes from appsettings at runtime.
/// </summary>
internal sealed class OrdersDbContextDesignTimeFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrdersDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=referenceapp_orders;Username=postgres;Password=postgres",
            npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "orders"));

        return new OrdersDbContext(optionsBuilder.Options);
    }
}
