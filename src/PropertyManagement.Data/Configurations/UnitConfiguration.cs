using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Data.Configurations;

internal class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.Property(u => u.UnitNumber).HasMaxLength(Unit.UnitNumberMaxLength).IsRequired();
        builder.Property(u => u.MonthlyRent).HasPrecision(18, 2);

        builder.HasIndex(u => new { u.PropertyId, u.UnitNumber }).IsUnique();

        builder.HasOne(u => u.UnitType)
            .WithMany()
            .HasForeignKey(u => u.UnitTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Leases)
            .WithOne()
            .HasForeignKey(l => l.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
