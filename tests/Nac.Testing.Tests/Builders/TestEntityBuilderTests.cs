using FluentAssertions;
using Nac.Testing.Tests.TestHelpers;
using Xunit;

namespace Nac.Testing.Tests.Builders;

public class TestEntityBuilderTests
{
    [Fact]
    public void Build_CreatesEntity()
    {
        var entity = new SampleEntityBuilder().Build();

        entity.Should().NotBeNull();
        entity.Id.Should().NotBe(Guid.Empty);
        entity.Name.Should().Be("Test");
    }

    [Fact]
    public void WithName_SetsName()
    {
        var entity = new SampleEntityBuilder()
            .WithName("Custom Name")
            .Build();

        entity.Name.Should().Be("Custom Name");
    }

    [Fact]
    public void Fluent_ChainsCorrectly()
    {
        var id = Guid.NewGuid();
        var entity = new SampleEntityBuilder()
            .WithId(id)
            .WithName("Chained")
            .Build();

        entity.Id.Should().Be(id);
        entity.Name.Should().Be("Chained");
    }

    [Fact]
    public void WithProperty_AppliesViaReflection()
    {
        var entity = new SampleEntityBuilder()
            .WithProperty("Name", "ViaReflection")
            .Build();

        // WithProperty applies AFTER CreateEntity, so it overrides builder field
        entity.Name.Should().Be("ViaReflection");
    }
}
