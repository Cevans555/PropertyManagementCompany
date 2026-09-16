using Microsoft.EntityFrameworkCore;

namespace PropertyManagement.Data.Queries;

public sealed class LeaseQueries
{
    private readonly PropertyManagementDbContext _db;

    public LeaseQueries(PropertyManagementDbContext db)
    {
        _db = db;
    }

    public Task<bool> UnitHasActiveLeaseAsync(int unitId, DateOnly today, CancellationToken cancellationToken = default)
    {
        return _db.Leases.AnyAsync(
            l => l.UnitId == unitId && l.StartDate <= today && l.EndDate >= today,
            cancellationToken);
    }

    public Task<bool> HasConflictingLeaseAsync(
        int unitId, DateOnly startDate, DateOnly endDate, DateOnly today, CancellationToken cancellationToken = default)
    {
        return _db.Leases.AnyAsync(
            l => l.UnitId == unitId
                && ((l.StartDate <= today && l.EndDate >= today)
                    || (l.StartDate <= endDate && l.EndDate >= startDate)),
            cancellationToken);
    }
}
