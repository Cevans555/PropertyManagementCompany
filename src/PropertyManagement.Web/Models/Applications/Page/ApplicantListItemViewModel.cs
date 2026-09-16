namespace PropertyManagement.Web.Models.Applications.Page;

public sealed record ApplicantListItemViewModel
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required bool IsPrimary { get; init; }
    public required bool HasSavedDetails { get; init; }
}
