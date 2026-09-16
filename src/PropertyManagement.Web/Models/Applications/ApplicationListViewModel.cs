namespace PropertyManagement.Web.Models.Applications;

public sealed record ApplicationListViewModel(IReadOnlyList<ApplicationListRowViewModel> Rows, bool IsManager);
