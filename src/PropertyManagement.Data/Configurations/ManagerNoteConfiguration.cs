using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Data.Configurations;

internal class ManagerNoteConfiguration : IEntityTypeConfiguration<ManagerNote>
{
    public void Configure(EntityTypeBuilder<ManagerNote> builder)
    {
        builder.Property(n => n.Text).HasMaxLength(ManagerNote.TextMaxLength).IsRequired();

        builder.HasOne<RentalApplication>()
            .WithMany()
            .HasForeignKey(n => n.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
