namespace Nac.Core.WebApi;

/// <summary>
/// Pagination parameters for list queries.
/// Clamps Page (min 1) and PageSize (1–100, default 20).
/// </summary>
public sealed record PaginationRequest(int Page = 1, int PageSize = 20)
{
    public int Page { get; init; } = Page < 1 ? 1 : Page;
    public int PageSize { get; init; } = PageSize is < 1 or > 100 ? 20 : PageSize;

    public int Skip => (Page - 1) * PageSize;
    public int Take => PageSize;
}
