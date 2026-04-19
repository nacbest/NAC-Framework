using FluentAssertions;
using Microsoft.Extensions.Options;
using Nac.MultiTenancy.Management.Abstractions;
using Nac.MultiTenancy.Management.Dtos;
using Nac.MultiTenancy.Management.Validators;
using Xunit;

namespace Nac.MultiTenancy.Management.Tests.Validators;

public class TenantRequestValidatorsTests
{
    private readonly CreateTenantRequestValidator _create = new();
    private readonly UpdateTenantRequestValidator _update = new();
    private readonly BulkTenantRequestValidator _bulk =
        new(Options.Create(new TenantManagementOptions { MaxBulkSize = 100 }));

    [Theory]
    [InlineData("acme")]
    [InlineData("acme-corp")]
    [InlineData("a01")]
    public void Identifier_Valid(string id)
    {
        var r = _create.Validate(new CreateTenantRequest(id, "Name", TenantIsolationMode.Shared, null, null));
        r.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Acme")]      // uppercase
    [InlineData("ab")]        // too short
    [InlineData("-acme")]     // leading hyphen
    [InlineData("acme!")]     // illegal char
    public void Identifier_Invalid(string id)
    {
        var r = _create.Validate(new CreateTenantRequest(id, "Name", TenantIsolationMode.Shared, null, null));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Database_RequiresConnectionString()
    {
        var r = _create.Validate(new CreateTenantRequest("acme", "Acme", TenantIsolationMode.Database, null, null));
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTenantRequest.ConnectionString));
    }

    [Fact]
    public void Update_NameBlank_Invalid()
    {
        var r = _update.Validate(new UpdateTenantRequest("", null, null, null));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Update_SwitchToDatabaseWithoutCs_Invalid()
    {
        var r = _update.Validate(new UpdateTenantRequest(null, TenantIsolationMode.Database, null, null));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Bulk_Empty_Invalid()
    {
        var r = _bulk.Validate(new BulkTenantRequest([]));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Bulk_TooMany_Invalid()
    {
        var ids = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var r = _bulk.Validate(new BulkTenantRequest(ids));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Bulk_Duplicates_Invalid()
    {
        var dup = Guid.NewGuid();
        var r = _bulk.Validate(new BulkTenantRequest([dup, dup]));
        r.IsValid.Should().BeFalse();
    }
}
