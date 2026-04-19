using FluentAssertions;
using Nac.Observability.Diagnostics;
using Xunit;

namespace Nac.Observability.Tests.Diagnostics;

public class NacActivitySourcesTests
{
    [Fact]
    public void Cqrs_HasExpectedName() =>
        NacActivitySources.Cqrs.Should().Be("Nac.Cqrs");

    [Fact]
    public void Persistence_HasExpectedName() =>
        NacActivitySources.Persistence.Should().Be("Nac.Persistence");

    [Fact]
    public void EventBus_HasExpectedName() =>
        NacActivitySources.EventBus.Should().Be("Nac.EventBus");

    [Fact]
    public void Identity_HasExpectedName() =>
        NacActivitySources.Identity.Should().Be("Nac.Identity");

    [Fact]
    public void MultiTenancy_HasExpectedName() =>
        NacActivitySources.MultiTenancy.Should().Be("Nac.MultiTenancy");

    [Fact]
    public void Caching_HasExpectedName() =>
        NacActivitySources.Caching.Should().Be("Nac.Caching");

    [Fact]
    public void Jobs_HasExpectedName() =>
        NacActivitySources.Jobs.Should().Be("Nac.Jobs");

    [Fact]
    public void All_ContainsAllSources()
    {
        NacActivitySources.All.Should().HaveCount(7);
        NacActivitySources.All.Should().Contain(
        [
            NacActivitySources.Cqrs,
            NacActivitySources.Persistence,
            NacActivitySources.EventBus,
            NacActivitySources.Identity,
            NacActivitySources.MultiTenancy,
            NacActivitySources.Caching,
            NacActivitySources.Jobs
        ]);
    }
}
