namespace PropertyManagement.Web.Models.Grid;

/// <summary>One page of rows plus the total number of rows matching the filters, across every page.</summary>
public sealed record PagedResult<T>
{
    /// <summary>The rows on this page.</summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>Rows matching the filters, across all pages.</summary>
    public required int TotalCount { get; init; }

    /// <summary>The 1-based page returned. A page past the end is clamped to the last page.</summary>
    public required int Page { get; init; }

    /// <summary>The maximum number of rows per page.</summary>
    public required int PageSize { get; init; }

    public int TotalPages => PageMath.TotalPages(TotalCount, PageSize);
}
