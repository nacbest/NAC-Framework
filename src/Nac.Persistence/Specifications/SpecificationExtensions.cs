using Nac.Core.Domain;

namespace Nac.Persistence.Specifications;

/// <summary>
/// Extension methods that bridge <see cref="Specification{T}"/> to <see cref="IQueryable{T}"/>,
/// enabling EF Core to translate specifications into SQL predicates.
/// </summary>
public static class SpecificationExtensions
{
    /// <summary>
    /// Filters an <see cref="IQueryable{T}"/> using the predicate defined by a
    /// <see cref="Specification{T}"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="queryable">The source queryable.</param>
    /// <param name="specification">The specification whose expression is applied as a WHERE clause.</param>
    /// <returns>A filtered <see cref="IQueryable{T}"/>.</returns>
    public static IQueryable<T> Where<T>(
        this IQueryable<T> queryable,
        Specification<T> specification) where T : class
        => queryable.Where(specification.ToExpression());
}
