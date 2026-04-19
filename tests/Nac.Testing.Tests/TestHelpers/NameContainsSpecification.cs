using System.Linq.Expressions;
using Nac.Core.Domain;

namespace Nac.Testing.Tests.TestHelpers;

public sealed class NameContainsSpecification(string search) : Specification<SampleEntity>
{
    public override Expression<Func<SampleEntity, bool>> ToExpression() =>
        entity => entity.Name.Contains(search);
}
