using PropertyManagement.Core.Enums;

namespace PropertyManagement.Web.Models.Applications;

public sealed record StatusHistoryRowViewModel
{
    public ApplicationStatus? FromStatus { get; init; }
    public required ApplicationStatus ToStatus { get; init; }
    public required string ChangedBy { get; init; }
    public required DateTime ChangedAt { get; init; }
    public string? Comment { get; init; }
}
