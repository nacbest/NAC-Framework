using Nac.Core.Primitives;

namespace Nac.Persistence.Tests.Helpers;

/// <summary>
/// Test entity implementing IAuditableEntity for auditing interceptor tests.
/// </summary>
public class TestAuditableEntity : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
