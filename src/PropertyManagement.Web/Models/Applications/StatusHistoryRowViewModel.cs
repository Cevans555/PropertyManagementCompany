using PropertyManagement.Core.Enums;

namespace PropertyManagement.Web.Models.Applications;

public sealed record StatusHistoryRowViewModel(
    ApplicationStatus? FromStatus,
    ApplicationStatus ToStatus,
    string ChangedBy,
    DateTime ChangedAt,
    string? Comment);
