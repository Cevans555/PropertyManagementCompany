using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Data.Services;

public sealed class ApplicationUpdater
{
    public const string StaleDataMessage = "This application was changed by someone else. Reload the page and try again.";
    public const string ConcurrentLeaseMessage = "Another change to this unit was saved at the same time. Please try again.";
    public const string NotFoundMessage = "Application not found.";

    private const int SqlDeadlockErrorNumber = 1205;

    private readonly PropertyManagementDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ApplicationUpdater> _logger;

    public ApplicationUpdater(PropertyManagementDbContext db, TimeProvider timeProvider, ILogger<ApplicationUpdater> logger)
    {
        _db = db;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public DateTime Now()
    {
        return _timeProvider.GetUtcNow().UtcDateTime;
    }

    public Task<ServiceResult> ApplyAsync(
        int applicationId, Action<RentalApplication, DateTime> change, CancellationToken cancellationToken)
    {
        return UpdateAsync(applicationId, (application, now) =>
        {
            change(application, now);
            return Task.CompletedTask;
        }, cancellationToken);
    }

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
            _logger.LogWarning("Change to application {ApplicationId} refused: {Reason}", applicationId, ex.Message);
            return Fail(ex.Message);
        }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException or StaleDataException)
        {
            _logger.LogWarning("Change to application {ApplicationId} refused because the page was out of date", applicationId);
            return Fail(StaleDataMessage);
        }
        catch (Exception ex) when (IsDeadlock(ex))
        {
            _logger.LogWarning("Change to application {ApplicationId} lost a deadlock to a concurrent lease change", applicationId);
            return Fail(ConcurrentLeaseMessage);
        }
    }

    public ServiceResult Fail(string error)
    {
        _db.ChangeTracker.Clear();
        return ServiceResult.Failure(error);
    }

    // EF wraps a cancelled transaction, so the SQL deadlock error can sit several levels down.
    private static bool IsDeadlock(Exception? ex)
    {
        while (ex is not null)
        {
            if (ex is SqlException { Number: SqlDeadlockErrorNumber })
                return true;

            ex = ex.InnerException;
        }

        return false;
    }
}
