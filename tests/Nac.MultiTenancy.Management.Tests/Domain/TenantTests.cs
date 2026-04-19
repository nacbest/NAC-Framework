using FluentAssertions;
using Nac.MultiTenancy.Management.Abstractions;
using Nac.MultiTenancy.Management.Domain;
using Nac.MultiTenancy.Management.Domain.Events;
using Xunit;

namespace Nac.MultiTenancy.Management.Tests.Domain;

public class TenantTests
{
    [Fact]
    public void Create_HappyPath_RaisesCreatedEvent()
    {
        var id = Guid.NewGuid();
        var tenant = Tenant.Create(id, "acme", "Acme Corp", TenantIsolationMode.Shared, null, null);

        tenant.Id.Should().Be(id);
        tenant.Identifier.Should().Be("acme");
        tenant.Name.Should().Be("Acme Corp");
        tenant.IsActive.Should().BeTrue();
        tenant.DomainEvents.Should().ContainSingle(e => e is TenantCreatedEvent);
    }

    [Fact]
    public void Create_EmptyIdentifier_Throws()
    {
        var act = () => Tenant.Create(Guid.NewGuid(), "  ", "Acme", TenantIsolationMode.Shared, null, null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_NoEvent()
    {
        var t = Tenant.Create(Guid.NewGuid(), "acme", "Acme", TenantIsolationMode.Shared, null, null);
        t.ClearDomainEvents();

        t.Activate();

        t.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Deactivate_ThenActivate_EmitsBoth()
    {
        var t = Tenant.Create(Guid.NewGuid(), "acme", "Acme", TenantIsolationMode.Shared, null, null);
        t.ClearDomainEvents();

        t.Deactivate();
        t.Activate();

        t.DomainEvents.Should().Contain(e => e is TenantDeactivatedEvent);
        t.DomainEvents.Should().Contain(e => e is TenantActivatedEvent);
    }

    [Fact]
    public void MarkDeleted_SetsFlagAndRaisesEvent()
    {
        var t = Tenant.Create(Guid.NewGuid(), "acme", "Acme", TenantIsolationMode.Shared, null, null);
        t.ClearDomainEvents();

        t.MarkDeleted();

        t.IsDeleted.Should().BeTrue();
        t.DomainEvents.Should().ContainSingle(e => e is TenantDeletedEvent);
    }

    [Fact]
    public void Rename_SameName_NoEvent()
    {
        var t = Tenant.Create(Guid.NewGuid(), "acme", "Acme", TenantIsolationMode.Shared, null, null);
        t.ClearDomainEvents();

        t.Rename("Acme");

        t.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void SetProperties_MergesAndRaises()
    {
        var t = Tenant.Create(Guid.NewGuid(), "acme", "Acme", TenantIsolationMode.Shared, null,
            new Dictionary<string, string?> { ["region"] = "us" });
        t.ClearDomainEvents();

        t.SetProperties(new Dictionary<string, string?> { ["region"] = "eu", ["tier"] = "gold" });

        t.Properties["region"].Should().Be("eu");
        t.Properties["tier"].Should().Be("gold");
        t.DomainEvents.Should().ContainSingle(e => e is TenantUpdatedEvent);
    }

    [Fact]
    public void ChangeIsolation_DatabaseWithoutCipher_Throws()
    {
        var t = Tenant.Create(Guid.NewGuid(), "acme", "Acme", TenantIsolationMode.Shared, null, null);
        var act = () => t.ChangeIsolation(TenantIsolationMode.Database, null);
        act.Should().Throw<InvalidOperationException>();
    }
}
