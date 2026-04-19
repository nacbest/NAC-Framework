using Microsoft.EntityFrameworkCore;
using Nac.Identity.Context;

namespace Nac.Identity.IntegrationTests.Infrastructure;

/// <summary>Concrete <see cref="NacIdentityDbContext"/> used by integration tests.</summary>
public sealed class TestIdentityDbContext : NacIdentityDbContext
{
    public TestIdentityDbContext(DbContextOptions<TestIdentityDbContext> options) : base(options) { }
}
