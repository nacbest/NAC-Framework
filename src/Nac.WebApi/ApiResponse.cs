using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Nac.WebApi;

/// <summary>Metadata included in every API response.</summary>
public sealed record ResponseMeta
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string? TraceId { get; init; } = Activity.Current?.Id;
}

/// <summary>
/// Standard success envelope.
/// <code>{ "data": {...}, "meta": { "timestamp": "...", "traceId": "..." } }</code>
/// </summary>
public sealed record ApiResponse<T>
{
    public required T Data { get; init; }
    public ResponseMeta Meta { get; init; } = new();

    public static ApiResponse<T> Success(T data) => new() { Data = data };
}

/// <summary>
/// Paginated response envelope.
/// <code>{ "data": [...], "pagination": {...}, "meta": {...} }</code>
/// </summary>
public sealed record PaginatedResponse<T>
{
    public required IReadOnlyList<T> Data { get; init; }
    public required PaginationMeta Pagination { get; init; }
    public ResponseMeta Meta { get; init; } = new();

    public static PaginatedResponse<T> Create(
        IReadOnlyList<T> items, int page, int pageSize, int total)
        => new()
        {
            Data = items,
            Pagination = new PaginationMeta(page, pageSize, total),
        };
}

/// <summary>Pagination metadata.</summary>
public sealed record PaginationMeta(int Page, int PageSize, int Total)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
}

/// <summary>
/// Standard error envelope.
/// <code>{ "error": { "code": "...", "message": "...", "details": [...] }, "meta": {...} }</code>
/// </summary>
public sealed record ErrorResponse
{
    public required ApiError Error { get; init; }
    public ResponseMeta Meta { get; init; } = new();
}

/// <summary>Top-level error object.</summary>
public sealed record ApiError(
    string Code,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<ApiErrorDetail>? Details = null);

/// <summary>Field-level validation error detail.</summary>
public sealed record ApiErrorDetail(string Field, string Message);
