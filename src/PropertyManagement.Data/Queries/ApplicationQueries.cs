using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Enums;

namespace PropertyManagement.Data.Queries;

public sealed class ApplicationQueries
{
    private readonly PropertyManagementDbContext _db;

    public ApplicationQueries(PropertyManagementDbContext db)
    {
        _db = db;
    }

    public Task<int?> OpenApplicationIdAsync(int unitId, string userId, CancellationToken cancellationToken = default)
    {
        return _db.RentalApplications
            .Where(a => a.UnitId == unitId
                && a.Applicants.Any(p => p.UserId == userId)
                && a.Status != ApplicationStatus.Approved
                && a.Status != ApplicationStatus.Denied
                && a.Status != ApplicationStatus.Withdrawn)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<int?> ApplicationIdForResidenceAsync(int residenceId, CancellationToken cancellationToken = default)
    {
        return _db.ResidenceHistories
            .Where(r => r.Id == residenceId)
            .Select(r => (int?)r.RentalApplicationId)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
