namespace Billing.Domain;

/// <summary>
/// Represents a billing customer. Created lazily on first order from that user.
/// Simple class — no AggregateRoot needed for this KISS module.
/// TenantId is stamped explicitly (defense-in-depth alongside TenantEntityInterceptor).
/// </summary>
internal sealed class Customer
{
    public Guid   Id        { get; init; }
    public Guid   UserId    { get; init; }
    public string Email     { get; init; } = string.Empty;
    public string TenantId  { get; init; } = string.Empty;
    public Plan   Plan      { get; set; }  = Plan.Free;
    public DateTime CreatedAt { get; init; }
}
