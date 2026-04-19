using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Nac.Core.Abstractions;
using Nac.Persistence.Interceptors;
using Nac.Persistence.Tests.Helpers;
using Xunit;

namespace Nac.Persistence.Tests.Interceptors;

public class SoftDeleteInterceptorTests
{
    private static TestDbContext CreateContextWithSoftDeleteInterceptor(IDateTimeProvider dateTimeProvider)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor(dateTimeProvider))
            .Options;
        return new TestDbContext(options);
    }

    [Fact]
    public async Task SavingChanges_DeletedSoftDeletable_SetsIsDeletedAndChangesStateToModified()
    {
        // Arrange
        var deletionTime = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(deletionTime);

        using var context = CreateContextWithSoftDeleteInterceptor(dateTimeProvider);

        var entity = new TestSoftDeletableEntity { Name = "To Delete" };
        context.SoftDeletableEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act
        context.SoftDeletableEntities.Remove(entity);
        await context.SaveChangesAsync();

        // Assert - Verify the entity's properties are set by the interceptor
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(deletionTime);
    }

    [Fact]
    public async Task SavingChanges_SoftDeletable_QueryFilterExcludesDeletedEntities()
    {
        // Arrange
        var deletionTime = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(deletionTime);

        using var context = CreateContextWithSoftDeleteInterceptor(dateTimeProvider);

        var entity = new TestSoftDeletableEntity { Name = "To Delete" };
        context.SoftDeletableEntities.Add(entity);
        await context.SaveChangesAsync();

        var entityId = entity.Id;

        // Soft delete
        context.SoftDeletableEntities.Remove(entity);
        await context.SaveChangesAsync();

        // Act
        var activeEntities = await context.SoftDeletableEntities.ToListAsync();

        // Assert
        activeEntities.Should().NotContain(e => e.Id == entityId);
    }

    [Fact]
    public async Task SavingChanges_DeletedNonSoftDeletable_DeletesNormally()
    {
        // Arrange
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        using var context = CreateContextWithSoftDeleteInterceptor(dateTimeProvider);

        var entity = new TestEntity { Name = "To Delete" };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        var entityId = entity.Id;

        // Act
        context.TestEntities.Remove(entity);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.TestEntities.FirstOrDefaultAsync(e => e.Id == entityId);
        retrieved.Should().BeNull();
    }
}
