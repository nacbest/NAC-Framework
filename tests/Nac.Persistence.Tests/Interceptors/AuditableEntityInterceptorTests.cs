using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Nac.Core.Abstractions;
using Nac.Core.Abstractions.Identity;
using Nac.Persistence.Interceptors;
using Nac.Persistence.Tests.Helpers;
using Xunit;

namespace Nac.Persistence.Tests.Interceptors;

public class AuditableEntityInterceptorTests
{
    private static TestDbContext CreateContextWithAuditInterceptor(
        IDateTimeProvider dateTimeProvider,
        IServiceProvider serviceProvider)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntityInterceptor(serviceProvider, dateTimeProvider))
            .Options;
        return new TestDbContext(options);
    }

    [Fact]
    public async Task SavingChanges_AddedAuditableEntity_SetsCreatedAtAndCreatedBy()
    {
        // Arrange
        var fixedTime = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(fixedTime);

        var userId = Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(userId);

        var services = new ServiceCollection();
        services.AddSingleton(currentUser);
        var serviceProvider = services.BuildServiceProvider();

        using var context = CreateContextWithAuditInterceptor(dateTimeProvider, serviceProvider);

        var entity = new TestAuditableEntity { Name = "Test" };

        // Act
        context.AuditableEntities.Add(entity);
        await context.SaveChangesAsync();

        // Assert
        entity.CreatedAt.Should().Be(fixedTime);
        entity.CreatedBy.Should().Be(userId.ToString());
        entity.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task SavingChanges_ModifiedAuditableEntity_SetsUpdatedAt()
    {
        // Arrange
        var creationTime = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var updateTime = new DateTime(2025, 1, 16, 14, 45, 0, DateTimeKind.Utc);

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(creationTime);

        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        using var context = CreateContextWithAuditInterceptor(dateTimeProvider, serviceProvider);

        var entity = new TestAuditableEntity { Name = "Test" };
        context.AuditableEntities.Add(entity);
        await context.SaveChangesAsync();

        // Change time and modify entity
        dateTimeProvider.UtcNow.Returns(updateTime);

        entity.Name = "Updated";

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.CreatedAt.Should().Be(creationTime);
        entity.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public async Task SavingChanges_WithoutCurrentUserService_DoesNotThrow()
    {
        // Arrange
        var fixedTime = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(fixedTime);

        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        using var context = CreateContextWithAuditInterceptor(dateTimeProvider, serviceProvider);

        var entity = new TestAuditableEntity { Name = "Test" };

        // Act & Assert
        context.AuditableEntities.Add(entity);
        await context.SaveChangesAsync();

        entity.CreatedAt.Should().Be(fixedTime);
        entity.CreatedBy.Should().BeNull();
    }
}
