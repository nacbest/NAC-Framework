using Nac.Core.DataSeeding;

namespace Orders.DataSeeding;

/// <summary>
/// Seed data for the Orders module.
/// V1: no-op — OrderStatus is a C# enum stored as string; no lookup table needed.
/// Future: seed default order configurations, test orders, etc.
/// </summary>
internal sealed class OrdersDataSeeder : IDataSeeder
{
    public Task SeedAsync(DataSeedContext context)
    {
        // No seed data required for v1.
        return Task.CompletedTask;
    }
}
