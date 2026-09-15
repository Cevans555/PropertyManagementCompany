using PropertyManagement.Core.Common;

namespace PropertyManagement.Core.Entities;

public class Unit : AuditableEntity
{
    public const int UnitNumberMaxLength = 20;
    public const int MaxBedrooms = 10;

    private readonly List<Lease> _leases = [];

    private Unit()
    {
        UnitNumber = null!;
        Property = null!;
        UnitType = null!;
    }

    internal Unit(Property property, string unitNumber, int bedrooms, decimal monthlyRent, UnitType unitType)
    {
        if (!unitType.IsActive)
            throw new DomainException($"Unit type '{unitType.Name}' is inactive and can't be selected.");

        Property = property;
        UnitNumber = null!;
        UnitType = null!;
        SetDetails(unitNumber, bedrooms, monthlyRent, unitType);
    }

    public int Id { get; private set; }
    public int PropertyId { get; private set; }
    public Property Property { get; private set; }
    public string UnitNumber { get; private set; }
    public int Bedrooms { get; private set; }
    public decimal MonthlyRent { get; private set; }
    public int UnitTypeId { get; private set; }
    public UnitType UnitType { get; private set; }
    public IReadOnlyCollection<Lease> Leases => _leases;

    /// <summary>Requires <see cref="Leases"/> to be loaded. Queries should use <see cref="Lease.Covers"/> logic directly.</summary>
    public bool HasActiveLeaseOn(DateOnly date) => _leases.Any(l => l.Covers(date));

    internal void Update(string unitNumber, int bedrooms, decimal monthlyRent, UnitType unitType)
    {
        // An inactive type may stay on a unit that already has it, but can't be newly chosen.
        var keepsCurrentType = ReferenceEquals(unitType, UnitType) || (unitType.Id != 0 && unitType.Id == UnitTypeId);
        if (!keepsCurrentType && !unitType.IsActive)
            throw new DomainException($"Unit type '{unitType.Name}' is inactive and can't be selected.");

        SetDetails(unitNumber, bedrooms, monthlyRent, unitType);
    }

    private void SetDetails(string unitNumber, int bedrooms, decimal monthlyRent, UnitType unitType)
    {
        if (bedrooms is < 0 or > MaxBedrooms)
            throw new DomainException($"Bedrooms must be between 0 and {MaxBedrooms}.");
        if (monthlyRent <= 0)
            throw new DomainException("Monthly rent must be greater than zero.");

        UnitNumber = Guard.Required(unitNumber, "Unit number", UnitNumberMaxLength);
        Bedrooms = bedrooms;
        MonthlyRent = monthlyRent;
        UnitType = unitType;
        UnitTypeId = unitType.Id;
    }
}