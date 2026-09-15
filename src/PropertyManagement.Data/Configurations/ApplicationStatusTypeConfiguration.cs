using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Data.Configurations;

internal class ApplicationStatusTypeConfiguration : IEntityTypeConfiguration<ApplicationStatusType>
{
    public void Configure(EntityTypeBuilder<ApplicationStatusType> builder)
    {
        builder.ToTable("ApplicationStatuses");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Name).HasMaxLength(50).IsRequired();

        builder.HasData(ApplicationStatusType.FromEnum());
    }
}
