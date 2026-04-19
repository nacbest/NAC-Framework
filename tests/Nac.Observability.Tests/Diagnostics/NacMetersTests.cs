using FluentAssertions;
using Nac.Observability.Diagnostics;
using Xunit;

namespace Nac.Observability.Tests.Diagnostics;

public class NacMetersTests
{
    [Fact]
    public void Cqrs_HasExpectedName() =>
        NacMeters.Cqrs.Should().Be("Nac.Cqrs");

    [Fact]
    public void Persistence_HasExpectedName() =>
        NacMeters.Persistence.Should().Be("Nac.Persistence");

    [Fact]
    public void EventBus_HasExpectedName() =>
        NacMeters.EventBus.Should().Be("Nac.EventBus");

    [Fact]
    public void Caching_HasExpectedName() =>
        NacMeters.Caching.Should().Be("Nac.Caching");

    [Fact]
    public void Jobs_HasExpectedName() =>
        NacMeters.Jobs.Should().Be("Nac.Jobs");

    [Fact]
    public void All_ContainsAllMeters()
    {
        NacMeters.All.Should().HaveCount(5);
        NacMeters.All.Should().Contain(
        [
            NacMeters.Cqrs,
            NacMeters.Persistence,
            NacMeters.EventBus,
            NacMeters.Caching,
            NacMeters.Jobs
        ]);
    }
}
