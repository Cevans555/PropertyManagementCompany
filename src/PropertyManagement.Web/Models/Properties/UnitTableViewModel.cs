namespace PropertyManagement.Web.Models.Properties;

public sealed record UnitTableViewModel(int PropertyId, IReadOnlyList<UnitRowViewModel> Units);
