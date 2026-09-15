using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PropertyManagement.Data.Configurations;

internal class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.FirstName).HasMaxLength(AppUser.NameMaxLength).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(AppUser.NameMaxLength).IsRequired();
    }
}
