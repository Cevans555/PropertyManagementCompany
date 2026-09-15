using PropertyManagement.Core.ValueObjects;

namespace PropertyManagement.Core.Entities;

public sealed record ResidenceDetails(
    Address Address,
    string LandlordName,
    string LandlordPhone,
    DateOnly MoveInDate,
    DateOnly MoveOutDate);
