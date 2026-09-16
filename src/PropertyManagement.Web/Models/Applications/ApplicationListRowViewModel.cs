using PropertyManagement.Core.Enums;

namespace PropertyManagement.Web.Models.Applications;

public sealed record ApplicationListRowViewModel(
    int Id,
    string PropertyName,
    string UnitNumber,
    ApplicationStatus Status,
    string Applicants,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    string? ClaimedBy);
