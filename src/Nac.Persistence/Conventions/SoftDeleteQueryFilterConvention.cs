using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Nac.Domain;

namespace Nac.Persistence.Conventions;

/// <summary>
/// EF Core model-building convention that automatically applies a global query filter
/// (<c>WHERE IsDeleted = false</c>) to every entity type that implements <see cref="ISoftDeletable"/>.
/// Registered in <see cref="NacDbContext.ConfigureConventions"/> so it runs for every
/// entity type added to the model — no per-entity configuration needed.
/// </summary>
internal sealed class SoftDeleteQueryFilterConvention : IEntityTypeAddedConvention
{
    public void ProcessEntityTypeAdded(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionContext<IConventionEntityTypeBuilder> context)
    {
        var clrType = entityTypeBuilder.Metadata.ClrType;

        if (!typeof(ISoftDeletable).IsAssignableFrom(clrType))
            return;

        // Build: e => e.IsDeleted == false
        var parameter = Expression.Parameter(clrType, "e");
        var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
        var body = Expression.Equal(property, Expression.Constant(false));
        var lambda = Expression.Lambda(body, parameter);

        entityTypeBuilder.HasQueryFilter(lambda);
    }
}
