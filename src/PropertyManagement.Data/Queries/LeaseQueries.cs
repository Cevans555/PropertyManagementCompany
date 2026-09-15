using Microsoft.EntityFrameworkCore;

namespace PropertyManagement.Data.Queries;

/// <summary>
/// Lease reads the domain can't do itself, because they need the database.
/// Queries read; services own saving and transactions.
/// </summary>
public sealed class LeaseQueries
{
    private readonly PropertyManagementDbContext _db;

    public LeaseQueries(PropertyManagementDbContext db)
    {
        _db = db;
    }

    /// <summary>A unit is unavailable while a lease covers today.</summary>
    public Task<bool> UnitHasActiveLeaseAsync(int unitId, DateOnly today, CancellationToken cancellationToken = default)
    {
        return _db.Leases.AnyAsync(
            l => l.UnitId == unitId && l.StartDate <= today && l.EndDate >= today,
            cancellationToken);
    }

    /// <summary>A lease covering today, or one overlapping the term being approved, blocks approval.</summary>
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
