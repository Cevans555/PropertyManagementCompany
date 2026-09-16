using System.Data;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Entities;
using PropertyManagement.Data.Queries;

namespace PropertyManagement.Data.Services;

public sealed class ApplicationReviewService
{
    private readonly ApplicationUpdater _updater;
    private readonly LeaseQueries _leases;
    private readonly TimeProvider _timeProvider;

    public ApplicationReviewService(ApplicationUpdater updater, LeaseQueries leases, TimeProvider timeProvider)
    {
        _updater = updater;
        _leases = leases;
        _timeProvider = timeProvider;
    }

    public Task<ServiceResult> ClaimAsync(int applicationId, string managerId, CancellationToken cancellationToken = default)
    {
        return _updater.ApplyAsync(applicationId, (application, now) => application.Claim(managerId, now), cancellationToken);
    }

    public Task<ServiceResult> ReleaseAsync(int applicationId, string managerId, CancellationToken cancellationToken = default)
    {
        return _updater.ApplyAsync(applicationId, (application, now) => application.Release(managerId, now), cancellationToken);
    }

    public Task<ServiceResult> ReturnAsync(int applicationId, string managerId, string comment, CancellationToken cancellationToken = default)
    {
        return _updater.ApplyAsync(applicationId, (application, now) => application.Return(managerId, comment, now), cancellationToken);
    }

    public Task<ServiceResult> DenyAsync(int applicationId, string managerId, string comment, CancellationToken cancellationToken = default)
    {
        return _updater.ApplyAsync(applicationId, (application, now) => application.Deny(managerId, comment, now), cancellationToken);
    }

    public Task<ServiceResult> ApproveAsync(
        int applicationId, string managerId, DateOnly leaseStartDate, string? comment, CancellationToken cancellationToken = default)
    {
        return _updater.UpdateAsync(applicationId, async (application, now) =>
        {
            var today = _timeProvider.Today();
            var leaseEndDate = Lease.EndDateFor(leaseStartDate);
            var hasConflictingLease = await _leases.HasConflictingLeaseAsync(
                application.UnitId, leaseStartDate, leaseEndDate, today, cancellationToken);

            application.Approve(managerId, leaseStartDate, today, hasConflictingLease, comment, now);
        }, cancellationToken, IsolationLevel.Serializable);
    }
}
