using System.Linq.Expressions;

namespace Nac.Core.Domain;

public abstract class Specification<T> where T : class
{
    private Func<T, bool>? _compiledExpression;

    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity)
    {
        _compiledExpression ??= ToExpression().Compile();
        return _compiledExpression(entity);
    }

    public Specification<T> And(Specification<T> other) =>
        new AndSpecification<T>(this, other);

    public Specification<T> Or(Specification<T> other) =>
        new OrSpecification<T>(this, other);

    public Specification<T> Not() =>
        new NotSpecification<T>(this);
}

/// <summary>
/// Replaces parameters in expressions to enable EF Core-compatible composition.
/// </summary>
internal sealed class ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam)
    : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node) =>
        node == oldParam ? newParam : base.VisitParameter(node);
}

internal sealed class AndSpecification<T>(Specification<T> left, Specification<T> right)
    : Specification<T> where T : class
{
    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = left.ToExpression();
        var rightExpr = right.ToExpression();
        var param = Expression.Parameter(typeof(T));
        var leftBody = new ParameterReplacer(leftExpr.Parameters[0], param).Visit(leftExpr.Body);
        var rightBody = new ParameterReplacer(rightExpr.Parameters[0], param).Visit(rightExpr.Body);
        var body = Expression.AndAlso(leftBody, rightBody);
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}

internal sealed class OrSpecification<T>(Specification<T> left, Specification<T> right)
    : Specification<T> where T : class
{
    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = left.ToExpression();
        var rightExpr = right.ToExpression();
        var param = Expression.Parameter(typeof(T));
        var leftBody = new ParameterReplacer(leftExpr.Parameters[0], param).Visit(leftExpr.Body);
        var rightBody = new ParameterReplacer(rightExpr.Parameters[0], param).Visit(rightExpr.Body);
        var body = Expression.OrElse(leftBody, rightBody);
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}

internal sealed class NotSpecification<T>(Specification<T> spec)
    : Specification<T> where T : class
{
    public override Expression<Func<T, bool>> ToExpression()
    {
        var expr = spec.ToExpression();
        var param = Expression.Parameter(typeof(T));
        var body = Expression.Not(new ParameterReplacer(expr.Parameters[0], param).Visit(expr.Body));
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}
