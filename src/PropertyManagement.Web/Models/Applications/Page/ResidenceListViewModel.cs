namespace PropertyManagement.Web.Models.Applications.Page;

public sealed record ResidenceListViewModel
{
    public required int ApplicationId { get; init; }
    public required bool Editable { get; init; }
    public required Guid SectionVersion { get; init; }
    public required IReadOnlyList<ResidenceRowViewModel> Residences { get; init; }
}
