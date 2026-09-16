using System.Data;
using Microsoft.Extensions.Logging;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Entities;
using PropertyManagement.Data.Queries;

namespace PropertyManagement.Data.Services;

public sealed class ApplicationReviewService
{
    private readonly ApplicationUpdater _updater;
    private readonly LeaseQueries _leases;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ApplicationReviewService> _logger;

    public ApplicationReviewService(
        ApplicationUpdater updater, LeaseQueries leases, TimeProvider timeProvider, ILogger<ApplicationReviewService> logger)
    {
        _updater = updater;
        _leases = leases;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ServiceResult> ClaimAsync(int applicationId, string managerId, CancellationToken cancellationToken = default)
    {
        var result = await _updater.ApplyAsync(applicationId, (application, now) => application.Claim(managerId, now), cancellationToken);
        LogIfSucceeded(result, "claimed", applicationId, managerId);
        return result;
    }

    public async Task<ServiceResult> ReleaseAsync(int applicationId, string managerId, CancellationToken cancellationToken = default)
    {
        var result = await _updater.ApplyAsync(applicationId, (application, now) => application.Release(managerId, now), cancellationToken);
        LogIfSucceeded(result, "released", applicationId, managerId);
        return result;
    }

    public async Task<ServiceResult> ReturnAsync(int applicationId, string managerId, string comment, CancellationToken cancellationToken = default)
    {
        var result = await _updater.ApplyAsync(applicationId, (application, now) => application.Return(managerId, comment, now), cancellationToken);
        LogIfSucceeded(result, "returned", applicationId, managerId);
        return result;
    }

    public async Task<ServiceResult> DenyAsync(int applicationId, string managerId, string comment, CancellationToken cancellationToken = default)
    {
        var result = await _updater.ApplyAsync(applicationId, (application, now) => application.Deny(managerId, comment, now), cancellationToken);
        LogIfSucceeded(result, "denied", applicationId, managerId);
        return result;
    }

    public async Task<ServiceResult> ApproveAsync(
        int applicationId, string managerId, DateOnly leaseStartDate, string? comment, CancellationToken cancellationToken = default)
    {
        var result = await _updater.UpdateAsync(applicationId, async (application, now) =>
        {
            var today = _timeProvider.Today();
            var leaseEndDate = Lease.EndDateFor(leaseStartDate);
            var hasConflictingLease = await _leases.HasConflictingLeaseAsync(
                application.UnitId, leaseStartDate, leaseEndDate, today, cancellationToken);

            application.Approve(managerId, leaseStartDate, today, hasConflictingLease, comment, now);
        }, cancellationToken, IsolationLevel.Serializable);

        if (result.Succeeded)
        {
            _logger.LogInformation(
                "Application {ApplicationId} approved by manager {ManagerId} with a lease starting {LeaseStartDate}",
                applicationId, managerId, leaseStartDate);
        }

        return result;
    }

    private void LogIfSucceeded(ServiceResult result, string action, int applicationId, string managerId)
    {
        if (result.Succeeded)
            _logger.LogInformation("Application {ApplicationId} {Action} by manager {ManagerId}", applicationId, action, managerId);
    }
}
