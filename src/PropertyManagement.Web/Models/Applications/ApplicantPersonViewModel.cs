namespace PropertyManagement.Web.Models.Applications;

public sealed record ApplicantPersonViewModel(
    string UserId,
    string AccountName,
    string AccountEmail,
    bool IsPrimary,
    bool IsCurrentUser,
    bool HasSavedDetails,
    string? FirstName,
    string? LastName,
    string? Phone,
    string? Email,
    string? CurrentAddress);
