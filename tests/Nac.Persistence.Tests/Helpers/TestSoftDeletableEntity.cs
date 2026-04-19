using Nac.Core.Primitives;

namespace Nac.Persistence.Tests.Helpers;

/// <summary>
/// Test entity implementing ISoftDeletable for soft-delete interceptor tests.
/// </summary>
public class TestSoftDeletableEntity : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
