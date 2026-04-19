using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Nac.Core.Results;
using Nac.MultiTenancy.Management.Abstractions;
using Nac.MultiTenancy.Management.Domain;
using Nac.MultiTenancy.Management.Dtos;
using Nac.MultiTenancy.Management.Services;
using Nac.MultiTenancy.Management.Tests.TestHelpers;
using Xunit;

namespace Nac.MultiTenancy.Management.Tests.Services;

public class TenantManagementServiceTests
{
    [Fact]
    public async Task Create_HappyPath_PersistsAndEncrypts()
    {
        var (svc, db, _, dp) = TestDbContextFactory.CreateService();

        var result = await svc.CreateAsync(new CreateTenantRequest(
            "acme", "Acme", TenantIsolationMode.Database, "Host=acme-db", null));

        result.IsSuccess.Should().BeTrue();
        var stored = db.Tenants.Single();
        stored.EncryptedConnectionString.Should().NotBe("Host=acme-db");
        var protector = dp.CreateProtector(EncryptedConnectionStringResolver.ProtectorPurpose);
        protector.Unprotect(stored.EncryptedConnectionString!).Should().Be("Host=acme-db");
    }

    [Fact]
    public async Task Create_DuplicateIdentifier_Conflict()
    {
        var (svc, _, _, _) = TestDbContextFactory.CreateService();
        await svc.CreateAsync(new CreateTenantRequest("acme", "Acme", TenantIsolationMode.Shared, null, null));

        var dup = await svc.CreateAsync(new CreateTenantRequest("acme", "Acme2", TenantIsolationMode.Shared, null, null));

        dup.Status.Should().Be(ResultStatus.Conflict);
    }

    [Fact]
    public async Task Create_InvalidIdentifier_ReturnsValidationError()
    {
        var (svc, _, _, _) = TestDbContextFactory.CreateService();

        var bad = await svc.CreateAsync(new CreateTenantRequest("Bad!", "X", TenantIsolationMode.Shared, null, null));

        bad.Status.Should().Be(ResultStatus.Invalid);
        bad.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        var (svc, _, _, _) = TestDbContextFactory.CreateService();

        var r = await svc.UpdateAsync(Guid.NewGuid(), new UpdateTenantRequest("New", null, null, null));

        r.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Update_Rename_Persists()
    {
        var (svc, db, _, _) = TestDbContextFactory.CreateService();
        var created = await svc.CreateAsync(new CreateTenantRequest("acme", "Old", TenantIsolationMode.Shared, null, null));
        db.ChangeTracker.Clear();

        var r = await svc.UpdateAsync(created.Value.Id, new UpdateTenantRequest("New", null, null, null));

        r.IsSuccess.Should().BeTrue();
        r.Value.Name.Should().Be("New");
    }

    [Fact]
    public async Task Update_SwitchToDatabaseWithoutCs_ValidationError()
    {
        var (svc, _, _, _) = TestDbContextFactory.CreateService();
        var created = await svc.CreateAsync(new CreateTenantRequest("acme", "Acme", TenantIsolationMode.Shared, null, null));

        var r = await svc.UpdateAsync(created.Value.Id, new UpdateTenantRequest(null, TenantIsolationMode.Database, null, null));

        r.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task Delete_HappyPath_SoftDeletes()
    {
        var (svc, db, _, _) = TestDbContextFactory.CreateService();
        var created = await svc.CreateAsync(new CreateTenantRequest("acme", "Acme", TenantIsolationMode.Shared, null, null));

        var del = await svc.DeleteAsync(created.Value.Id);

        del.IsSuccess.Should().BeTrue();
        db.Tenants.Count().Should().Be(0);
        db.Tenants.IgnoreQueryFilters().Single().IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task List_Pagination_Clamps()
    {
        var (svc, _, _, _) = TestDbContextFactory.CreateService();
        for (var i = 0; i < 5; i++)
            await svc.CreateAsync(new CreateTenantRequest($"t-{i:00}", $"T{i}", TenantIsolationMode.Shared, null, null));

        var page = await svc.ListAsync(new TenantListQuery(Page: 1, PageSize: 200));

        page.PageSize.Should().Be(100);
        page.Items.Count.Should().Be(5);
        page.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task List_Search_FiltersByIdentifier()
    {
        var (svc, _, _, _) = TestDbContextFactory.CreateService();
        await svc.CreateAsync(new CreateTenantRequest("acme", "Acme", TenantIsolationMode.Shared, null, null));
        await svc.CreateAsync(new CreateTenantRequest("globex", "Globex", TenantIsolationMode.Shared, null, null));

        var page = await svc.ListAsync(new TenantListQuery(Search: "acme"));

        page.Items.Should().ContainSingle().Which.Identifier.Should().Be("acme");
    }

    [Fact]
    public async Task BulkActivate_MixedSucceedFail_ReportsFailures()
    {
        var (svc, db, _, _) = TestDbContextFactory.CreateService();
        var ok = await svc.CreateAsync(new CreateTenantRequest("acme", "Acme", TenantIsolationMode.Shared, null, null));
        await svc.DeactivateAsync(ok.Value.Id);
        var missing = Guid.NewGuid();

        var result = await svc.BulkActivateAsync([ok.Value.Id, missing]);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRequested.Should().Be(2);
        result.Value.Succeeded.Should().Be(1);
        result.Value.Failures.Should().ContainKey(missing);
    }
}
