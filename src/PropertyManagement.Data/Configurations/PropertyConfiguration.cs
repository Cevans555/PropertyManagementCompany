using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Data.Configurations;

internal class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(Property.NameMaxLength).IsRequired();

        builder.OwnsOne(p => p.Address, a => a.MapAddress());
        builder.Navigation(p => p.Address).IsRequired();

        builder.HasMany(p => p.Units)
            .WithOne(u => u.Property)
            .HasForeignKey(u => u.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
