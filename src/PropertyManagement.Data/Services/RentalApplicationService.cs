using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Entities;
using PropertyManagement.Data.Queries;

namespace PropertyManagement.Data.Services;

public sealed class RentalApplicationService
{
    public const string UnitNotFoundMessage = "Unit not found.";

    private readonly PropertyManagementDbContext _db;
    private readonly ApplicationUpdater _updater;
    private readonly LeaseQueries _leases;
    private readonly TimeProvider _timeProvider;

    public RentalApplicationService(
        PropertyManagementDbContext db,
        ApplicationUpdater updater,
        LeaseQueries leases,
        TimeProvider timeProvider)
    {
        _db = db;
        _updater = updater;
        _leases = leases;
        _timeProvider = timeProvider;
    }

    public async Task<ServiceResult<int>> StartAsync(int unitId, string applicantUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var unitExists = await _db.Units.AnyAsync(u => u.Id == unitId, cancellationToken);
            if (!unitExists)
                return ServiceResult<int>.Failure(UnitNotFoundMessage);

            var hasActiveLease = await _leases.UnitHasActiveLeaseAsync(unitId, _timeProvider.Today(), cancellationToken);
            var application = RentalApplication.Start(unitId, applicantUserId, hasActiveLease, _updater.Now());

            _db.RentalApplications.Add(application);
            await _db.SaveChangesAsync(cancellationToken);

            return ServiceResult<int>.Success(application.Id);
        }
        catch (DomainException ex)
        {
            _updater.Fail(ex.Message);
            return ServiceResult<int>.Failure(ex.Message);
        }
    }

    public Task<ServiceResult> SubmitAsync(int applicationId, string applicantUserId, CancellationToken cancellationToken = default)
    {
        return _updater.UpdateAsync(applicationId, async (application, now) =>
        {
            var hasActiveLease = await _leases.UnitHasActiveLeaseAsync(application.UnitId, _timeProvider.Today(), cancellationToken);
            application.Submit(applicantUserId, hasActiveLease, now);
        }, cancellationToken);
    }

    public Task<ServiceResult> WithdrawAsync(int applicationId, string applicantUserId, CancellationToken cancellationToken = default)
    {
        return _updater.ApplyAsync(applicationId, (application, now) => application.Withdraw(applicantUserId, now), cancellationToken);
    }
}
