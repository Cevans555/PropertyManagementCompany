namespace PropertyManagement.Web.Models.Applications;

public sealed record AvailableUnitViewModel(
    int UnitId,
    string PropertyName,
    string City,
    string State,
    string UnitNumber,
    string UnitTypeName,
    int Bedrooms,
    decimal MonthlyRent,
    int? OpenApplicationId);
