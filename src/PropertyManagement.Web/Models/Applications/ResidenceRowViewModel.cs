namespace PropertyManagement.Web.Models.Applications;

public sealed record ResidenceRowViewModel(
    int Id,
    string Address,
    string LandlordName,
    string LandlordPhone,
    DateOnly MoveInDate,
    DateOnly MoveOutDate);
