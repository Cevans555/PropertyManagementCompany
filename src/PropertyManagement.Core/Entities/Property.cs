using PropertyManagement.Core.Common;
using PropertyManagement.Core.ValueObjects;

namespace PropertyManagement.Core.Entities;

public class Property : AuditableEntity
{
    public const int NameMaxLength = 150;

    private readonly List<Unit> _units = [];

    public int Id { get; private set; }
    public string Name { get; private set; }
    public Address Address { get; private set; }
    public IReadOnlyCollection<Unit> Units => _units;

    private Property()
    {
        Name = null!;
        Address = null!;
    }

    public Property(string name, Address address)
    {
        Name = Guard.Required(name, "Name", NameMaxLength);
        Address = address ?? throw new DomainException("Address is required.");
    }

    public void Update(string name, Address address)
    {
        Name = Guard.Required(name, "Name", NameMaxLength);
        Address = address ?? throw new DomainException("Address is required.");
    }

    public Unit AddUnit(string unitNumber, int bedrooms, decimal monthlyRent, UnitType unitType)
    {
        EnsureUnitNumberIsUnique(unitNumber, except: null);

        var unit = new Unit(this, unitNumber, bedrooms, monthlyRent, unitType);
        _units.Add(unit);
        return unit;
    }

    public void UpdateUnit(Unit unit, string unitNumber, int bedrooms, decimal monthlyRent, UnitType unitType)
    {
        if (!_units.Contains(unit))
            throw new DomainException("The unit does not belong to this property.");

        EnsureUnitNumberIsUnique(unitNumber, except: unit);
        unit.Update(unitNumber, bedrooms, monthlyRent, unitType);
    }

    private void EnsureUnitNumberIsUnique(string? unitNumber, Unit? except)
    {
        var normalized = unitNumber?.Trim();
        if (_units.Any(u => u != except && string.Equals(u.UnitNumber, normalized, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Unit {normalized} already exists at this property.");
    }
}