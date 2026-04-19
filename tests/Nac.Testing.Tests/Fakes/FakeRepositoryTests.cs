using FluentAssertions;
using Nac.Testing.Fakes;
using Nac.Testing.Tests.TestHelpers;
using Xunit;

namespace Nac.Testing.Tests.Fakes;

public class FakeRepositoryTests
{
    [Fact]
    public async Task Add_StoresEntity()
    {
        var repo = new FakeRepository<SampleEntity>();
        var entity = new SampleEntity { Name = "Alpha" };

        await repo.AddAsync(entity);

        repo.Items.Should().ContainSingle().Which.Should().Be(entity);
    }

    [Fact]
    public async Task Add_TracksInAddedItems()
    {
        var repo = new FakeRepository<SampleEntity>();
        var entity = new SampleEntity { Name = "Beta" };

        await repo.AddAsync(entity);

        repo.AddedItems.Should().ContainSingle().Which.Should().Be(entity);
    }

    [Fact]
    public async Task GetById_ExistingEntity_Returns()
    {
        var id = Guid.NewGuid();
        var entity = new SampleEntity { Id = id, Name = "Charlie" };
        var repo = new FakeRepository<SampleEntity>().WithItems(entity);

        var result = await repo.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNull()
    {
        var repo = new FakeRepository<SampleEntity>();

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_ReturnsAll()
    {
        var e1 = new SampleEntity { Name = "One" };
        var e2 = new SampleEntity { Name = "Two" };
        var repo = new FakeRepository<SampleEntity>().WithItems(e1, e2);

        var result = await repo.ListAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(e1);
        result.Should().Contain(e2);
    }

    [Fact]
    public async Task ListAsync_WithSpec_FiltersCorrectly()
    {
        var e1 = new SampleEntity { Name = "Alpha" };
        var e2 = new SampleEntity { Name = "Beta" };
        var e3 = new SampleEntity { Name = "Alphabet" };
        var repo = new FakeRepository<SampleEntity>().WithItems(e1, e2, e3);
        var spec = new NameContainsSpecification("Alpha");

        var result = await repo.ListAsync(spec);

        result.Should().HaveCount(2);
        result.Should().Contain(e1);
        result.Should().Contain(e3);
        result.Should().NotContain(e2);
    }

    [Fact]
    public async Task Delete_RemovesFromList()
    {
        var entity = new SampleEntity { Name = "ToDelete" };
        var repo = new FakeRepository<SampleEntity>().WithItems(entity);

        await repo.DeleteAsync(entity);

        repo.Items.Should().BeEmpty();
        repo.DeletedItems.Should().ContainSingle().Which.Should().Be(entity);
    }

    [Fact]
    public async Task WithItems_SeedsData()
    {
        var e1 = new SampleEntity { Name = "Seed1" };
        var e2 = new SampleEntity { Name = "Seed2" };

        var repo = new FakeRepository<SampleEntity>().WithItems(e1, e2);

        var items = await repo.ListAsync();
        items.Should().HaveCount(2);
        // Seeded items do NOT appear in AddedItems (seeding bypasses tracking)
        repo.AddedItems.Should().BeEmpty();
    }
}
