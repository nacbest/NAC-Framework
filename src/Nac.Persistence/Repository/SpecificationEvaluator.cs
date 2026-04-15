using Microsoft.EntityFrameworkCore;
using Nac.Core.Persistence;

namespace Nac.Persistence.Repository;

/// <summary>
/// Translates an <see cref="ISpecification{TEntity}"/> into an EF Core LINQ query.
/// Applies criteria (WHERE), includes (JOIN), ordering, and paging in order.
/// </summary>
internal static class SpecificationEvaluator
{
    public static IQueryable<TEntity> Evaluate<TEntity>(
        IQueryable<TEntity> inputQuery,
        ISpecification<TEntity> spec)
        where TEntity : class
    {
        var query = inputQuery;

        foreach (var criterion in spec.Criteria)
            query = query.Where(criterion);

        foreach (var include in spec.Includes)
            query = query.Include(include);

        foreach (var includeString in spec.IncludeStrings)
            query = query.Include(includeString);

        // Use ThenBy for subsequent orderings to avoid overwriting previous ones
        IOrderedQueryable<TEntity>? ordered = null;
        foreach (var order in spec.OrderExpressions)
        {
            if (ordered is null)
            {
                ordered = order.Direction == OrderDirection.Ascending
                    ? query.OrderBy(order.KeySelector)
                    : query.OrderByDescending(order.KeySelector);
            }
            else
            {
                ordered = order.Direction == OrderDirection.Ascending
                    ? ordered.ThenBy(order.KeySelector)
                    : ordered.ThenByDescending(order.KeySelector);
            }
        }

        if (ordered is not null)
            query = ordered;

        if (spec.IsPagingEnabled && spec.Skip.HasValue && spec.Take.HasValue)
            query = query.Skip(spec.Skip.Value).Take(spec.Take.Value);

        return query;
    }
}
