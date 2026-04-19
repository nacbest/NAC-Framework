using Microsoft.EntityFrameworkCore;
using Nac.Identity.Context;

namespace Nac.Identity.Tests.Infrastructure;

/// <summary>Concrete <see cref="NacIdentityDbContext"/> used by InMemory unit tests.</summary>
public sealed class TestIdentityDbContext : NacIdentityDbContext
{
    public TestIdentityDbContext(DbContextOptions<TestIdentityDbContext> options) : base(options) { }
}
