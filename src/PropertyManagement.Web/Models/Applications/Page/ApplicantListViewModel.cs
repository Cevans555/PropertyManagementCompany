namespace PropertyManagement.Web.Models.Applications.Page;

public sealed record ApplicantListViewModel
{
    public required int ApplicationId { get; init; }
    public required bool Editable { get; init; }
    public required IReadOnlyList<ApplicantListItemViewModel> People { get; init; }
}
