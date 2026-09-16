using PropertyManagement.Core.Enums;

namespace PropertyManagement.Web.Models.Applications;

public sealed record ApplicationHeaderViewModel(
    int Id,
    ApplicationStatus Status,
    string PropertyName,
    string UnitNumber,
    string UnitTypeName,
    int Bedrooms,
    decimal MonthlyRent,
    DateTime CreatedAt,
    DateTime? SubmittedAt);
