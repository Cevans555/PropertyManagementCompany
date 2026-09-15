using PropertyManagement.Core.Common;
using PropertyManagement.Core.Dtos;
using PropertyManagement.Core.ValueObjects;

namespace PropertyManagement.Core.Entities;

public class ResidenceHistory : AuditableEntity
{
    public const int LandlordNameMaxLength = 150;
    public const int LandlordPhoneMaxLength = 30;

    public int Id { get; private set; }
    public int RentalApplicationId { get; private set; }
    public Address Address { get; private set; }
    public string LandlordName { get; private set; }
    public string LandlordPhone { get; private set; }
    public DateOnly MoveInDate { get; private set; }
    public DateOnly MoveOutDate { get; private set; }

    private ResidenceHistory()
    {
        Address = null!;
        LandlordName = null!;
        LandlordPhone = null!;
    }

    internal ResidenceHistory(ResidenceDetails details) : this()
    {
        Apply(details);
    }

    internal void Apply(ResidenceDetails details)
    {
        if (details.MoveOutDate < details.MoveInDate)
            throw new DomainException("Move-out date can't be before the move-in date.");

        Address = details.Address ?? throw new DomainException("Address is required.");
        LandlordName = Guard.Required(details.LandlordName, "Landlord name", LandlordNameMaxLength);
        LandlordPhone = Guard.Required(details.LandlordPhone, "Landlord phone", LandlordPhoneMaxLength);
        MoveInDate = details.MoveInDate;
        MoveOutDate = details.MoveOutDate;
    }
}
