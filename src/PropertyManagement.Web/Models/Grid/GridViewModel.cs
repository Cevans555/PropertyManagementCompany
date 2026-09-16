namespace PropertyManagement.Web.Models.Grid;

public sealed record GridViewModel
{
    public required string Id { get; init; }

    public required string DataUrl { get; init; }

    public required string Caption { get; init; }

    public required IReadOnlyList<GridColumn> Columns { get; init; }

    public string DefaultSort { get; init; } = string.Empty;

    public SortDirection DefaultDirection { get; init; } = SortDirection.Asc;

    public int PageSize { get; init; } = 20;

    public string EmptyText { get; init; } = "No results.";

    public string? FilterFormId { get; init; }
}
