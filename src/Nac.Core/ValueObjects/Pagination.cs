using Nac.Core.Primitives;

namespace Nac.Core.ValueObjects;

public sealed class Pagination : ValueObject
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int PageNumber { get; }
    public int PageSize { get; }
    public int Skip => (PageNumber - 1) * PageSize;

    public Pagination(int pageNumber = 1, int pageSize = DefaultPageSize)
    {
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PageNumber;
        yield return PageSize;
    }
}
