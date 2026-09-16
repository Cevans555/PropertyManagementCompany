namespace PropertyManagement.Web.Models.Properties;

public sealed record PropertyListItemViewModel(
    int Id,
    string Name,
    string Street,
    string City,
    string State,
    string PostalCode,
    int UnitCount,
    int AvailableUnitCount);
