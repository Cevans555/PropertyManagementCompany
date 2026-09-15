using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Common.Interfaces;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Data;

public class PropertyManagementDbContext : IdentityDbContext<AppUser>
{
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<UnitType> UnitTypes => Set<UnitType>();
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<RentalApplication> RentalApplications => Set<RentalApplication>();
    public DbSet<Applicant> Applicants => Set<Applicant>();
    public DbSet<ResidenceHistory> ResidenceHistories => Set<ResidenceHistory>();
    public DbSet<StatusHistory> StatusHistories => Set<StatusHistory>();
    public DbSet<ManagerNote> ManagerNotes => Set<ManagerNote>();
    public DbSet<ApplicationStatusType> ApplicationStatuses => Set<ApplicationStatusType>();

    public PropertyManagementDbContext(DbContextOptions<PropertyManagementDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(PropertyManagementDbContext).Assembly);

        ConfigureAuditColumns(builder);
    }

    private static void ConfigureAuditColumns(ModelBuilder builder)
    {
        var auditedTypes = builder.Model.GetEntityTypes()
            .Where(t => !t.IsOwned() && typeof(ICreationAudited).IsAssignableFrom(t.ClrType))
            .ToList();

        foreach (var entityType in auditedTypes)
        {
            var entity = builder.Entity(entityType.ClrType);
            entity.Property(nameof(ICreationAudited.CreatedById)).HasMaxLength(450).IsRequired();

            if (typeof(IAuditable).IsAssignableFrom(entityType.ClrType))
                entity.Property(nameof(IAuditable.ModifiedById)).HasMaxLength(450);
        }
    }
}
