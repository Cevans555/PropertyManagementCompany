using PropertyManagement.Core.Common;
using PropertyManagement.Core.Entities;
using static PropertyManagement.Tests.TestSupport.TestData;

namespace PropertyManagement.Tests;
public class UnitTests
{
    [Fact]
    public void AddUnit_Throws_WhenUnitTypeIsInactive()
    {
        var property = NewProperty();

        Assert.Throws<DomainException>(() => property.AddUnit("101", 1, 1200m, new UnitType("Duplex", isActive: false)));
    }

    [Fact]
    public void UpdateUnit_AllowsKeepingInactiveType()
    {
        var property = NewProperty();
        var loft = new UnitType("Loft");
        var unit = property.AddUnit("101", 1, 1200m, loft);
        loft.Deactivate();

        property.UpdateUnit(unit, "101", 2, 1500m, loft);

        Assert.Equal(2, unit.Bedrooms);
        Assert.Same(loft, unit.UnitType);
    }

    [Fact]
    public void UpdateUnit_Throws_WhenSwitchingToInactiveType()
    {
        var property = NewProperty();
        var unit = property.AddUnit("101", 1, 1200m, new UnitType("Loft"));

        Assert.Throws<DomainException>(() =>
            property.UpdateUnit(unit, "101", 1, 1200m, new UnitType("Duplex", isActive: false)));
    }

    [Fact]
    public void AddUnit_Throws_WhenUnitNumberAlreadyExists()
    {
        var property = NewProperty();
        property.AddUnit("101", 1, 1200m, new UnitType("Loft"));

        Assert.Throws<DomainException>(() => property.AddUnit(" 101 ", 2, 1500m, new UnitType("Loft")));
    }

    [Theory]
    [InlineData(-1, 1200)]
    [InlineData(11, 1200)]
    [InlineData(1, 0)]
    public void AddUnit_Throws_ForInvalidDetails(int bedrooms, decimal rent)
    {
        var property = NewProperty();

        Assert.Throws<DomainException>(() => property.AddUnit("101", bedrooms, rent, new UnitType("Loft")));
    }

    private static Property NewProperty()
    {
        return new Property("Maple Commons", Address());
    }
}
