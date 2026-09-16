namespace PropertyManagement.Web.Models.Grid;

public sealed record GridColumn
{
    public required string Key { get; init; }

    public required string Title { get; init; }

    public string? SortKey { get; init; }

    public GridCellFormat Format { get; init; } = GridCellFormat.Text;

    public string? SecondaryKey { get; init; }

    public string? SecondaryPrefix { get; init; }

    public string? EmptyText { get; init; }

    public string? LinkText { get; init; }

    public IReadOnlyDictionary<string, string>? BadgeClasses { get; init; }

    public bool HideTitle { get; init; }
}
