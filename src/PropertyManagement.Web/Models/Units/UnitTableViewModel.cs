namespace PropertyManagement.Web.Models.Units;

public sealed record UnitTableViewModel
{
    public required int PropertyId { get; init; }
    public required IReadOnlyList<UnitRowViewModel> Units { get; init; }
}
