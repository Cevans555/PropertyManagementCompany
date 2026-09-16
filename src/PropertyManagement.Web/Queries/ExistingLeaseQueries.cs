using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Common;
using PropertyManagement.Data;
using PropertyManagement.Web.Models.Reviews;

namespace PropertyManagement.Web.Queries;

public sealed class ExistingLeaseQueries
{
    private readonly PropertyManagementDbContext _db;
    private readonly TimeProvider _timeProvider;

    public ExistingLeaseQueries(PropertyManagementDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<ExistingLeaseViewModel>> ForApplicationAsync(
        int applicationId, CancellationToken cancellationToken = default)
    {
        return await (
                from holding in Holdings()
                where holding.ApplicationId == applicationId
                join user in _db.Users on holding.HolderUserId equals user.Id
                join unit in _db.Units on holding.UnitId equals unit.Id
                orderby holding.StartDate, holding.LeaseApplicationId
                select new ExistingLeaseViewModel
                {
                    HolderName = user.FirstName + " " + user.LastName,
                    LeaseApplicationId = holding.LeaseApplicationId,
                    PropertyName = unit.Property.Name,
                    UnitNumber = unit.UnitNumber,
                    StartDate = holding.StartDate,
                    EndDate = holding.EndDate
                })
            .ToListAsync(cancellationToken);
    }

    public IQueryable<int> ApplicationIdsWithLeaseHolders()
    {
        return Holdings().Select(h => h.ApplicationId);
    }

    // The rule in one place: a lease counts while its term covers today or starts later, and never the
    // application's own lease.
    private IQueryable<LeaseHolding> Holdings()
    {
        var today = _timeProvider.Today();

        return from applicant in _db.Applicants
               join holder in _db.Applicants on applicant.UserId equals holder.UserId
               join lease in _db.Leases on holder.RentalApplicationId equals lease.RentalApplicationId
               where holder.RentalApplicationId != applicant.RentalApplicationId && lease.EndDate >= today
               select new LeaseHolding
               {
                   ApplicationId = applicant.RentalApplicationId,
                   HolderUserId = holder.UserId,
                   LeaseApplicationId = lease.RentalApplicationId,
                   UnitId = lease.UnitId,
                   StartDate = lease.StartDate,
                   EndDate = lease.EndDate
               };
    }
}
