namespace PropertyManagement.Web.Models.Units;

public sealed record UnitRowViewModel(
    int Id,
    string UnitNumber,
    string UnitTypeName,
    bool UnitTypeIsActive,
    int Bedrooms,
    decimal MonthlyRent,
    DateOnly? LeasedUntil,
    int ApplicationCount);
