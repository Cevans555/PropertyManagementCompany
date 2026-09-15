using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Data.Services;

/// <summary>
/// The steps every application change shares: load the application, run the change, save, and turn the
/// expected failures into friendly messages. The services above it decide what the change is.
/// </summary>
public sealed class ApplicationUpdater
{
    public const string StaleDataMessage = "This application was changed by someone else. Reload the page and try again.";
    public const string ConcurrentLeaseMessage = "Another change to this unit was saved at the same time. Please try again.";
    public const string NotFoundMessage = "Application not found.";

    private const int SqlDeadlockErrorNumber = 1205;

    private readonly PropertyManagementDbContext _db;
    private readonly TimeProvider _timeProvider;

    public ApplicationUpdater(PropertyManagementDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public DateTime Now()
    {
        return _timeProvider.GetUtcNow().UtcDateTime;
    }

    /// <summary>For changes that don't need to query the database first.</summary>
    public Task<ServiceResult> ApplyAsync(
        int applicationId, Action<RentalApplication, DateTime> change, CancellationToken cancellationToken)
    {
        return UpdateAsync(applicationId, (application, now) =>
        {
            change(application, now);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    /// <param name="isolationLevel">Set it to run the change in a transaction, e.g. serializable for approvals.</param>
    public async Task<ServiceResult> UpdateAsync(
        int applicationId,
        Func<RentalApplication, DateTime, Task> change,
        CancellationToken cancellationToken,
        IsolationLevel? isolationLevel = null)
    {
        try
        {
            await using var transaction = isolationLevel is null
                ? null
                : await _db.Database.BeginTransactionAsync(isolationLevel.Value, cancellationToken);

            var application = await _db.RentalApplications
                .Include(a => a.Applicants)
                .Include(a => a.Residences)
                .AsSplitQuery()
                .SingleOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

            if (application is null)
                return ServiceResult.Failure(NotFoundMessage);

            await change(application, Now());
            await _db.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            // A business rule said no, e.g. "only submitted applications can be claimed".
            return Fail(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrency token didn't match: someone else changed it since it was loaded.
            return Fail(StaleDataMessage);
        }
        catch (Exception ex) when (IsDeadlock(ex))
        {
            // Two serializable transactions collided; SQL Server picked one to cancel.
            return Fail(ConcurrentLeaseMessage);
        }
    }

    /// <summary>Discards the failed changes so a later save on this DbContext doesn't retry them.</summary>
    public ServiceResult Fail(string error)
    {
        _db.ChangeTracker.Clear();
        return ServiceResult.Failure(error);
    }

    private static bool IsDeadlock(Exception ex)
    {
        if (ex is SqlException { Number: SqlDeadlockErrorNumber })
            return true;

        return ex.InnerException is SqlException { Number: SqlDeadlockErrorNumber };
    }
}
