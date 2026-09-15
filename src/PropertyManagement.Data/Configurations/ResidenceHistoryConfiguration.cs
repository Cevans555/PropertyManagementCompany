using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Data.Configurations;

internal class ResidenceHistoryConfiguration : IEntityTypeConfiguration<ResidenceHistory>
{
    public void Configure(EntityTypeBuilder<ResidenceHistory> builder)
    {
        builder.OwnsOne(r => r.Address, a => a.MapAddress());
        builder.Navigation(r => r.Address).IsRequired();

        builder.Property(r => r.LandlordName).HasMaxLength(ResidenceHistory.LandlordNameMaxLength).IsRequired();
        builder.Property(r => r.LandlordPhone).HasMaxLength(ResidenceHistory.LandlordPhoneMaxLength).IsRequired();
    }
}
