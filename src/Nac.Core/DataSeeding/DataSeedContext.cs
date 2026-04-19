namespace Nac.Core.DataSeeding;

public sealed class DataSeedContext(IServiceProvider serviceProvider)
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    public string? TenantId { get; set; }
    public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>();
}
