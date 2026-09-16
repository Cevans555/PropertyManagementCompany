namespace PropertyManagement.Web.Models.Applications;

public sealed record ApplicantListViewModel(int ApplicationId, bool Editable, IReadOnlyList<ApplicantListItemViewModel> People);
