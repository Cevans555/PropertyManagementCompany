using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Core.ValueObjects;

namespace PropertyManagement.Data.Configurations;

internal static class AddressMapping
{
    public static void MapAddress<TOwner>(this OwnedNavigationBuilder<TOwner, Address> address)
        where TOwner : class
    {
        address.Property(a => a.Street).HasColumnName("Street").HasMaxLength(Address.StreetMaxLength);
        address.Property(a => a.City).HasColumnName("City").HasMaxLength(Address.CityMaxLength);
        address.Property(a => a.State).HasColumnName("State").HasMaxLength(Address.StateMaxLength);
        address.Property(a => a.PostalCode).HasColumnName("PostalCode").HasMaxLength(Address.PostalCodeMaxLength);
    }
}
