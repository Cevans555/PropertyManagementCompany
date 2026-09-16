namespace PropertyManagement.Web.Models.Applications;

public sealed record ApplicantPersonViewModel
{
    public required string UserId { get; init; }
    public required string AccountName { get; init; }
    public required string AccountEmail { get; init; }
    public required bool IsPrimary { get; init; }
    public required bool IsCurrentUser { get; init; }
    public required bool HasSavedDetails { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? CurrentAddress { get; init; }
}
