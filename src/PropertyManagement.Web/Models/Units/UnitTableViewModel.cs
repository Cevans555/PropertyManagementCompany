namespace PropertyManagement.Web.Models.Units;

public sealed record UnitTableViewModel(int PropertyId, IReadOnlyList<UnitRowViewModel> Units);
