using System.Linq.Expressions;

namespace Nac.Core.Persistence;

/// <summary>
/// Specification interface for encapsulating query logic.
/// Repositories accept specifications to build queries without exposing IQueryable.
/// </summary>
public interface ISpecification<TEntity> where TEntity : class
{
    IReadOnlyList<Expression<Func<TEntity, bool>>> Criteria { get; }
    IReadOnlyList<Expression<Func<TEntity, object>>> Includes { get; }
    IReadOnlyList<string> IncludeStrings { get; }
    IReadOnlyList<OrderExpression<TEntity>> OrderExpressions { get; }
    int? Skip { get; }
    int? Take { get; }
    bool IsPagingEnabled { get; }
}

/// <summary>Represents an ordering expression with direction.</summary>
public sealed record OrderExpression<TEntity>(
    Expression<Func<TEntity, object>> KeySelector,
    OrderDirection Direction
) where TEntity : class;

/// <summary>Ordering direction for specification queries.</summary>
public enum OrderDirection
{
    Ascending,
    Descending
}

/// <summary>
/// Base class for building specifications using a fluent protected API.
/// Subclass this to define reusable, composable query specifications.
/// </summary>
public abstract class Specification<TEntity> : ISpecification<TEntity> where TEntity : class
{
    private readonly List<Expression<Func<TEntity, bool>>> _criteria = [];
    private readonly List<Expression<Func<TEntity, object>>> _includes = [];
    private readonly List<string> _includeStrings = [];
    private readonly List<OrderExpression<TEntity>> _orderExpressions = [];

    public IReadOnlyList<Expression<Func<TEntity, bool>>> Criteria => _criteria;
    public IReadOnlyList<Expression<Func<TEntity, object>>> Includes => _includes;
    public IReadOnlyList<string> IncludeStrings => _includeStrings;
    public IReadOnlyList<OrderExpression<TEntity>> OrderExpressions => _orderExpressions;
    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    protected void Where(Expression<Func<TEntity, bool>> criteria)
        => _criteria.Add(criteria);

    protected void Include(Expression<Func<TEntity, object>> include)
        => _includes.Add(include);

    protected void Include(string navigationPropertyPath)
        => _includeStrings.Add(navigationPropertyPath);

    protected void OrderBy(Expression<Func<TEntity, object>> keySelector)
        => _orderExpressions.Add(new(keySelector, OrderDirection.Ascending));

    protected void OrderByDescending(Expression<Func<TEntity, object>> keySelector)
        => _orderExpressions.Add(new(keySelector, OrderDirection.Descending));

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }
}
