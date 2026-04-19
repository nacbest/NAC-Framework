using Nac.Cqrs.Queries;

namespace Nac.Cqrs.Tests.Helpers;

public sealed record TestQuery(int Id) : IQuery<string>;

public sealed class TestQueryHandler : IQueryHandler<TestQuery, string>
{
    public ValueTask<string> HandleAsync(TestQuery query, CancellationToken ct = default)
    {
        return ValueTask.FromResult($"Item-{query.Id}");
    }
}
