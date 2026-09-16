using PropertyManagement.Core.Enums;

namespace PropertyManagement.Web.Models.Applications.Page;

public sealed record ApplicationHeaderViewModel
{
    public required int Id { get; init; }
    public required ApplicationStatus Status { get; init; }
    public required string PropertyName { get; init; }
    public required string UnitNumber { get; init; }
    public required string UnitTypeName { get; init; }
    public required int Bedrooms { get; init; }
    public required decimal MonthlyRent { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
}
