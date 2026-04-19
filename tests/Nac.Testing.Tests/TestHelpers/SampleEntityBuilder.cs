using Nac.Testing.Builders;

namespace Nac.Testing.Tests.TestHelpers;

public sealed class SampleEntityBuilder : TestEntityBuilder<SampleEntity, SampleEntityBuilder>
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Test";

    public SampleEntityBuilder WithId(Guid id) { _id = id; return Self; }
    public SampleEntityBuilder WithName(string name) { _name = name; return Self; }
    protected override SampleEntity CreateEntity() => new() { Id = _id, Name = _name };
}
