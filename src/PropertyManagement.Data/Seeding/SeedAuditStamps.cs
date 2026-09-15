using PropertyManagement.Core.Common.Interfaces;

namespace PropertyManagement.Data.Seeding;

internal sealed class SeedAuditStamps
{
    private readonly List<(object Entity, string UserId, DateTime At)> _stamps = [];

    public void Add(object entity, string userId, DateTime at)
    {
        _stamps.Add((entity, userId, at));
    }

    public void ApplyTo(PropertyManagementDbContext db)
    {
        foreach (var (entity, userId, at) in _stamps)
        {
            var entry = db.Entry(entity);
            entry.Property(nameof(ICreationAudited.CreatedAt)).CurrentValue = at;
            entry.Property(nameof(ICreationAudited.CreatedById)).CurrentValue = userId;
        }

        _stamps.Clear();
    }
}
