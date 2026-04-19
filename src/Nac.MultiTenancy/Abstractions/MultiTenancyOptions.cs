namespace Nac.MultiTenancy.Abstractions;

public sealed class MultiTenancyOptions
{
    public string DefaultTenantId { get; set; } = "default";
    public List<Type> Strategies { get; set; } = [];
    public bool EnablePerTenantDatabase { get; set; }
}
