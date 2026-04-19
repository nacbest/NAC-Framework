using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Nac.Caching;
using Nac.Core.Abstractions;
using Nac.Core.Abstractions.Identity;
using Nac.Core.Abstractions.Permissions;
using Nac.Cqrs.Dispatching;
using Nac.EventBus.Abstractions;
using Nac.Testing.Fakes;
using Nac.Testing.Fixtures;

namespace Nac.Testing.Tests.Fixtures;

public class NacTestFixtureTests
{
    [Fact]
    public void AllFakes_Accessible()
    {
        using var fixture = new NacTestFixture();

        fixture.CurrentUser.Should().NotBeNull();
        fixture.DateTimeProvider.Should().NotBeNull();
        fixture.PermissionChecker.Should().NotBeNull();
        fixture.EventPublisher.Should().NotBeNull();
        fixture.Sender.Should().NotBeNull();
        fixture.Cache.Should().NotBeNull();
    }

    [Fact]
    public void GetService_ReturnsRegisteredFake()
    {
        using var fixture = new NacTestFixture();

        var currentUser = fixture.GetService<ICurrentUser>();
        var dateTime = fixture.GetService<IDateTimeProvider>();
        var permissions = fixture.GetService<IPermissionChecker>();
        var eventPublisher = fixture.GetService<IEventPublisher>();
        var sender = fixture.GetService<ISender>();
        var cache = fixture.GetService<INacCache>();

        currentUser.Should().BeSameAs(fixture.CurrentUser);
        dateTime.Should().BeSameAs(fixture.DateTimeProvider);
        permissions.Should().BeSameAs(fixture.PermissionChecker);
        eventPublisher.Should().BeSameAs(fixture.EventPublisher);
        sender.Should().BeSameAs(fixture.Sender);
        cache.Should().BeSameAs(fixture.Cache);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var fixture = new NacTestFixture();

        Action act = () => fixture.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void CustomConfigureServices_Applied()
    {
        // Subclass that registers an extra service
        using var fixture = new FixtureWithExtraService();

        var extra = fixture.GetService<ExtraService>();

        extra.Should().NotBeNull();
        extra.Value.Should().Be("injected");
    }

    // --- helpers ---

    private sealed class ExtraService
    {
        public string Value { get; } = "injected";
    }

    private sealed class FixtureWithExtraService : NacTestFixture
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ExtraService>();
        }
    }
}
