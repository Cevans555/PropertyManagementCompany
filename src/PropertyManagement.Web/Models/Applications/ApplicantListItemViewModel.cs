namespace PropertyManagement.Web.Models.Applications;

public sealed record ApplicantListItemViewModel(string Name, string Email, bool IsPrimary, bool HasSavedDetails);
