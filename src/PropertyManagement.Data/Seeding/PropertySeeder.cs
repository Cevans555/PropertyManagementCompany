using Bogus;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Data.Seeding;

internal static class PropertySeeder
{
    private const int PropertyCount = 5;

    private static readonly (string Name, bool IsActive, int? FixedBedrooms)[] UnitTypes =
    [
        ("Standard", true, null),
        ("Studio", true, 0),
        ("Garden", true, null),
        ("Penthouse", true, null),
        ("Balcony", true, null),
        ("Private Entrance", true, null),
        ("Loft", false, null)
    ];

    private static readonly string[] NameSuffixes = ["Apartments", "Commons", "Residences", "Place", "Lofts"];

    public static async Task<List<UnitType>> SeedUnitTypesAsync(PropertyManagementDbContext db, CancellationToken cancellationToken)
    {
        var existing = await db.UnitTypes.ToListAsync(cancellationToken);

        foreach (var (name, isActive, _) in UnitTypes)
        {
            var alreadyExists = existing.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
            if (alreadyExists)
                continue;

            var unitType = new UnitType(name, isActive);
            db.UnitTypes.Add(unitType);
            existing.Add(unitType);
        }

        await db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public static List<Property> CreateProperties(
        Faker faker, List<UnitType> unitTypes, List<AppUser> managers, DateTime now, SeedAuditStamps audit)
    {
        var bedroomsByType = UnitTypes.ToDictionary(t => t.Name, t => t.FixedBedrooms, StringComparer.OrdinalIgnoreCase);
        var knownTypes = unitTypes.Where(t => bedroomsByType.ContainsKey(t.Name)).ToList();
        var activeTypes = knownTypes.Where(t => t.IsActive).ToList();
        var retiredType = knownTypes.First(t => !t.IsActive);

        var properties = new List<Property>();

        for (var p = 0; p < PropertyCount; p++)
        {
            var name = $"{faker.Address.StreetName()} {faker.PickRandom(NameSuffixes)}";
            var property = new Property(name, FakeData.Address(faker));

            var manager = faker.PickRandom(managers);
            var createdAt = now.AddDays(-faker.Random.Int(200, 400));
            audit.Add(property, manager.Id, createdAt);

            var floor = faker.Random.Int(1, 3);
            var unitCount = faker.Random.Int(4, 8);
            for (var i = 1; i <= unitCount; i++)
            {
                var unitType = faker.PickRandom(activeTypes);
                var bedrooms = bedroomsByType[unitType.Name] ?? faker.Random.Int(1, 3);
                var unit = property.AddUnit($"{floor}{i:00}", bedrooms, FakeData.Rent(faker, bedrooms), unitType);
                audit.Add(unit, manager.Id, createdAt);
            }

            properties.Add(property);
        }

        GiveOneUnitARetiredType(faker, properties[0], retiredType, bedroomsByType);
        return properties;
    }

    private static void GiveOneUnitARetiredType(
        Faker faker, Property property, UnitType retiredType, Dictionary<string, int?> bedroomsByType)
    {
        retiredType.Activate();

        var unit = property.Units.Last();
        var bedrooms = bedroomsByType[retiredType.Name] ?? faker.Random.Int(1, 3);
        property.UpdateUnit(unit, unit.UnitNumber, bedrooms, FakeData.Rent(faker, bedrooms), retiredType);

        retiredType.Deactivate();
    }
}
