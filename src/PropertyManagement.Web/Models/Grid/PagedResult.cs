namespace PropertyManagement.Web.Models.Grid;

/// <summary>One page of rows plus the total number of rows matching the filters, across every page.</summary>
/// <param name="Items">The rows on this page.</param>
/// <param name="TotalCount">Rows matching the filters, across all pages.</param>
/// <param name="Page">The 1-based page returned. A page past the end is clamped to the last page.</param>
/// <param name="PageSize">The maximum number of rows per page.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageMath.TotalPages(TotalCount, PageSize);
}
