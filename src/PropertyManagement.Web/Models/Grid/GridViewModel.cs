namespace PropertyManagement.Web.Models.Grid;

public sealed record GridViewModel(string Id, string DataUrl, string Caption, IReadOnlyList<GridColumn> Columns)
{
    public string DefaultSort { get; init; } = string.Empty;

    public SortDirection DefaultDirection { get; init; } = SortDirection.Asc;

    public int PageSize { get; init; } = 20;

    public string EmptyText { get; init; } = "No results.";

    public string? FilterFormId { get; init; }
}
