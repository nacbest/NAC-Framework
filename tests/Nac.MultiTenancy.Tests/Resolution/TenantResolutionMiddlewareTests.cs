using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Nac.MultiTenancy.Abstractions;
using Nac.MultiTenancy.Resolution;
using Xunit;

namespace Nac.MultiTenancy.Tests.Resolution;

public class TenantResolutionMiddlewareTests
{
    private readonly ITenantStore _tenantStore = Substitute.For<ITenantStore>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly NullLogger<TenantResolutionMiddleware> _logger = NullLogger<TenantResolutionMiddleware>.Instance;

    private TenantResolutionMiddleware CreateMiddleware(RequestDelegate? next = null) =>
        new(next ?? (_ => Task.CompletedTask));

    [Fact]
    public async Task InvokeAsync_ResolvesFromFirstMatchingStrategy()
    {
        var strategy1 = Substitute.For<ITenantResolutionStrategy>();
        var strategy2 = Substitute.For<ITenantResolutionStrategy>();
        strategy1.ResolveAsync(Arg.Any<HttpContext>()).Returns((string?)null);
        strategy2.ResolveAsync(Arg.Any<HttpContext>()).Returns("tenant-2");

        var tenant = new TenantInfo { Id = "tenant-2", Name = "Tenant 2" };
        _tenantStore.GetByIdAsync("tenant-2", Arg.Any<CancellationToken>()).Returns(tenant);

        var middleware = CreateMiddleware();
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext, [strategy1, strategy2], _tenantStore, _tenantContext, _logger);

        await strategy1.Received(1).ResolveAsync(httpContext);
        await strategy2.Received(1).ResolveAsync(httpContext);
        _tenantContext.Received(1).SetCurrentTenant(tenant);
    }

    [Fact]
    public async Task InvokeAsync_SetsContextWhenTenantFound()
    {
        var strategy = Substitute.For<ITenantResolutionStrategy>();
        strategy.ResolveAsync(Arg.Any<HttpContext>()).Returns("tenant-1");

        var tenant = new TenantInfo { Id = "tenant-1", Name = "Tenant 1", IsActive = true };
        _tenantStore.GetByIdAsync("tenant-1", Arg.Any<CancellationToken>()).Returns(tenant);

        var middleware = CreateMiddleware();
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext, [strategy], _tenantStore, _tenantContext, _logger);

        _tenantContext.Received(1).SetCurrentTenant(Arg.Is<TenantInfo>(t => t.Id == "tenant-1"));
    }

    [Fact]
    public async Task InvokeAsync_DoesNotSetContextWhenTenantNotFound()
    {
        var strategy = Substitute.For<ITenantResolutionStrategy>();
        strategy.ResolveAsync(Arg.Any<HttpContext>()).Returns("unknown-tenant");
        _tenantStore.GetByIdAsync("unknown-tenant", Arg.Any<CancellationToken>()).Returns((TenantInfo?)null);

        var middleware = CreateMiddleware();
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext, [strategy], _tenantStore, _tenantContext, _logger);

        _tenantContext.DidNotReceive().SetCurrentTenant(Arg.Any<TenantInfo>());
    }

    [Fact]
    public async Task InvokeAsync_DoesNotSetContextWhenTenantInactive()
    {
        var strategy = Substitute.For<ITenantResolutionStrategy>();
        strategy.ResolveAsync(Arg.Any<HttpContext>()).Returns("tenant-inactive");

        var tenant = new TenantInfo { Id = "tenant-inactive", Name = "Inactive", IsActive = false };
        _tenantStore.GetByIdAsync("tenant-inactive", Arg.Any<CancellationToken>()).Returns(tenant);

        var middleware = CreateMiddleware();
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext, [strategy], _tenantStore, _tenantContext, _logger);

        _tenantContext.DidNotReceive().SetCurrentTenant(Arg.Any<TenantInfo>());
    }

    [Fact]
    public async Task InvokeAsync_CallsNextRegardlessOfResolution()
    {
        var strategy = Substitute.For<ITenantResolutionStrategy>();
        strategy.ResolveAsync(Arg.Any<HttpContext>()).Returns((string?)null);

        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext, [strategy], _tenantStore, _tenantContext, _logger);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_DoesNotSetContextWhenNoResolution()
    {
        var strategy = Substitute.For<ITenantResolutionStrategy>();
        strategy.ResolveAsync(Arg.Any<HttpContext>()).Returns((string?)null);

        var middleware = CreateMiddleware();
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext, [strategy], _tenantStore, _tenantContext, _logger);

        _tenantContext.DidNotReceive().SetCurrentTenant(Arg.Any<TenantInfo>());
    }
}
