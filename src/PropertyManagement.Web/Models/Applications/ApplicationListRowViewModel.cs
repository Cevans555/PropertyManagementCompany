using PropertyManagement.Core.Enums;

namespace PropertyManagement.Web.Models.Applications;

public sealed record ApplicationListRowViewModel
{
    public required int Id { get; init; }
    public required string PropertyName { get; init; }
    public required string UnitNumber { get; init; }
    public required ApplicationStatus Status { get; init; }
    public required string Applicants { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public string? ClaimedBy { get; init; }
}
