using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.ValueObjects;

namespace PropertyManagement.Data.Configurations;

internal class ApplicantConfiguration : IEntityTypeConfiguration<Applicant>
{
    public void Configure(EntityTypeBuilder<Applicant> builder)
    {
        builder.Property(a => a.FirstName).HasMaxLength(Applicant.NameMaxLength);
        builder.Property(a => a.LastName).HasMaxLength(Applicant.NameMaxLength);
        builder.Property(a => a.Phone).HasMaxLength(Applicant.PhoneMaxLength);
        builder.Property(a => a.Email).HasMaxLength(Applicant.EmailMaxLength);

        builder.Property(a => a.Street).HasMaxLength(Address.StreetMaxLength);
        builder.Property(a => a.City).HasMaxLength(Address.CityMaxLength);
        builder.Property(a => a.State).HasMaxLength(Address.StateMaxLength);
        builder.Property(a => a.PostalCode).HasMaxLength(Address.PostalCodeMaxLength);

        builder.Property(a => a.RowVersion).IsRowVersion();

        builder.HasIndex(a => new { a.RentalApplicationId, a.UserId }).IsUnique();

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
