using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Data.Configurations;

internal class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> builder)
    {
        builder.HasIndex(l => new { l.UnitId, l.StartDate, l.EndDate });

        builder.HasIndex(l => l.RentalApplicationId).IsUnique();
    }
}
