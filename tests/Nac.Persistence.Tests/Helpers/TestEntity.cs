namespace Nac.Persistence.Tests.Helpers;

/// <summary>
/// Simple test entity with no special interfaces.
/// </summary>
public class TestEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
}
