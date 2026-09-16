namespace PropertyManagement.Web.Models.Applications;

public sealed record ResidenceListViewModel(
    int ApplicationId,
    bool Editable,
    Guid SectionVersion,
    IReadOnlyList<ResidenceRowViewModel> Residences);
