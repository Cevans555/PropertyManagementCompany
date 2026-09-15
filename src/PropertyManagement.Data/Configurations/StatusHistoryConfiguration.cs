using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Data.Configurations;

internal class StatusHistoryConfiguration : IEntityTypeConfiguration<StatusHistory>
{
    public void Configure(EntityTypeBuilder<StatusHistory> builder)
    {
        builder.Property(h => h.FromStatus).HasColumnName("FromStatusId");
        builder.Property(h => h.ToStatus).HasColumnName("ToStatusId");
        builder.Property(h => h.Comment).HasMaxLength(StatusHistory.CommentMaxLength);

        builder.HasOne<ApplicationStatusType>()
            .WithMany()
            .HasForeignKey(h => h.FromStatus)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationStatusType>()
            .WithMany()
            .HasForeignKey(h => h.ToStatus)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(h => h.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
