using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Nac.MultiTenancy.Abstractions;
using Nac.MultiTenancy.Management.Services;
using NSubstitute;
using Xunit;

namespace Nac.MultiTenancy.Management.Tests.Services;

public class EncryptedConnectionStringResolverTests
{
    private static IConfiguration ConfigWithDefault(string defaultCs) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = defaultCs,
            })
            .Build();

    [Fact]
    public void Resolve_UnknownTenant_ReturnsDefault()
    {
        var store = Substitute.For<ITenantStore>();
        store.GetByIdAsync("acme", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(null));
        var sut = new EncryptedConnectionStringResolver(
            store, new EphemeralDataProtectionProvider(), ConfigWithDefault("Host=default"));

        var cs = sut.Resolve("acme");

        cs.Should().Be("Host=default");
    }

    [Fact]
    public void Resolve_EncryptedCs_RoundTripsPlaintext()
    {
        var dp = new EphemeralDataProtectionProvider();
        var cipher = dp.CreateProtector(EncryptedConnectionStringResolver.ProtectorPurpose)
            .Protect("Host=real-db");
        var store = Substitute.For<ITenantStore>();
        store.GetByIdAsync("acme", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(new TenantInfo
            {
                Id = "acme",
                Name = "Acme",
                ConnectionString = cipher,
            }));

        var sut = new EncryptedConnectionStringResolver(store, dp, ConfigWithDefault("Host=default"));

        sut.Resolve("acme").Should().Be("Host=real-db");
    }

    [Fact]
    public void Resolve_TenantWithoutCs_ReturnsDefault()
    {
        var store = Substitute.For<ITenantStore>();
        store.GetByIdAsync("acme", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(new TenantInfo
            {
                Id = "acme",
                Name = "Acme",
                ConnectionString = null,
            }));
        var sut = new EncryptedConnectionStringResolver(
            store, new EphemeralDataProtectionProvider(), ConfigWithDefault("Host=default"));

        sut.Resolve("acme").Should().Be("Host=default");
    }
}
