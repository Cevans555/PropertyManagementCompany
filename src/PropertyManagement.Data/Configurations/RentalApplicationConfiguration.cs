using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Data.Configurations;

internal class RentalApplicationConfiguration : IEntityTypeConfiguration<RentalApplication>
{
    public void Configure(EntityTypeBuilder<RentalApplication> builder)
    {
        builder.Property(a => a.Status).HasColumnName("StatusId").IsConcurrencyToken();
        builder.HasOne<ApplicationStatusType>()
            .WithMany()
            .HasForeignKey(a => a.Status)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => a.Status);

        builder.HasOne(a => a.Unit)
            .WithMany()
            .HasForeignKey(a => a.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(a => a.ClaimedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.ResidenceSectionVersion).IsConcurrencyToken();

        builder.HasOne(a => a.Lease)
            .WithOne()
            .HasForeignKey<Lease>(l => l.RentalApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Applicants)
            .WithOne()
            .HasForeignKey(x => x.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Residences)
            .WithOne()
            .HasForeignKey(x => x.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.StatusHistory)
            .WithOne()
            .HasForeignKey(x => x.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
