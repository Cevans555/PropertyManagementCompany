using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Dtos;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Security;
using PropertyManagement.Core.ValueObjects;
using PropertyManagement.Data.Queries;

namespace PropertyManagement.Data.Services;

public sealed class RentalApplicationService
{
    public const string UnitNotFoundMessage = "Unit not found.";
    public const string NoApplicantAccountMessage = "No applicant account uses that email address.";

    private readonly PropertyManagementDbContext _db;
    private readonly ApplicationUpdater _updater;
    private readonly LeaseQueries _leases;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RentalApplicationService> _logger;

    public RentalApplicationService(
        PropertyManagementDbContext db,
        ApplicationUpdater updater,
        LeaseQueries leases,
        TimeProvider timeProvider,
        ILogger<RentalApplicationService> logger)
    {
        _db = db;
        _updater = updater;
        _leases = leases;
        _timeProvider = timeProvider;
        _logger = logger;
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

            _logger.LogInformation("Application {ApplicationId} started for unit {UnitId} by user {UserId}", application.Id, unitId, applicantUserId);
            return ServiceResult<int>.Success(application.Id);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Application for unit {UnitId} refused: {Reason}", unitId, ex.Message);
            _updater.Fail(ex.Message);
            return ServiceResult<int>.Failure(ex.Message);
        }
    }

    public async Task<ServiceResult> SubmitAsync(int applicationId, string applicantUserId, CancellationToken cancellationToken = default)
    {
        var result = await _updater.UpdateAsync(applicationId, async (application, now) =>
        {
            var hasActiveLease = await _leases.UnitHasActiveLeaseAsync(application.UnitId, _timeProvider.Today(), cancellationToken);
            application.Submit(applicantUserId, hasActiveLease, now);
        }, cancellationToken);

        if (result.Succeeded)
            _logger.LogInformation("Application {ApplicationId} submitted by user {UserId}", applicationId, applicantUserId);

        return result;
    }

    public async Task<ServiceResult> WithdrawAsync(int applicationId, string applicantUserId, CancellationToken cancellationToken = default)
    {
        var result = await _updater.ApplyAsync(applicationId, (application, now) => application.Withdraw(applicantUserId, now), cancellationToken);

        if (result.Succeeded)
            _logger.LogInformation("Application {ApplicationId} withdrawn by user {UserId}", applicationId, applicantUserId);

        return result;
    }

    public Task<ServiceResult> SaveApplicantDetailsAsync(
        int applicationId, string applicantUserId, ApplicantDetails details, byte[] expectedRowVersion,
        bool allowInvalid = false, CancellationToken cancellationToken = default)
    {
        return _updater.ApplyAsync(applicationId, (application, now) =>
        {
            var applicant = application.Applicants.SingleOrDefault(a => a.UserId == applicantUserId);
            if (applicant is not null && !applicant.RowVersion.AsSpan().SequenceEqual(expectedRowVersion))
                throw new StaleDataException();

            application.SaveApplicantDetails(applicantUserId, applicantUserId, details, now, allowInvalid);
        }, cancellationToken);
    }

    public Task<ServiceResult> AddResidenceAsync(
        int applicationId, string applicantUserId, ResidenceInput input, Guid expectedSectionVersion,
        CancellationToken cancellationToken = default)
    {
        return _updater.ApplyAsync(applicationId, (application, _) =>
        {
            EnsureResidenceSectionVersion(application, expectedSectionVersion);
            application.AddResidence(applicantUserId, ToResidenceDetails(input));
        }, cancellationToken);
    }

    public Task<ServiceResult> UpdateResidenceAsync(
        int applicationId, string applicantUserId, int residenceId, ResidenceInput input, Guid expectedSectionVersion,
        CancellationToken cancellationToken = default)
    {
        return _updater.ApplyAsync(applicationId, (application, _) =>
        {
            EnsureResidenceSectionVersion(application, expectedSectionVersion);
            application.UpdateResidence(applicantUserId, residenceId, ToResidenceDetails(input));
        }, cancellationToken);
    }

    public Task<ServiceResult> RemoveResidenceAsync(
        int applicationId, string applicantUserId, int residenceId, Guid expectedSectionVersion,
        CancellationToken cancellationToken = default)
    {
        return _updater.ApplyAsync(applicationId, (application, _) =>
        {
            EnsureResidenceSectionVersion(application, expectedSectionVersion);
            application.RemoveResidence(applicantUserId, residenceId);
        }, cancellationToken);
    }

    public Task<ServiceResult> SaveResidenceSectionAsync(
        int applicationId, string applicantUserId, Guid expectedSectionVersion, bool allowInvalid = false,
        CancellationToken cancellationToken = default)
    {
        return _updater.ApplyAsync(applicationId, (application, now) =>
        {
            EnsureResidenceSectionVersion(application, expectedSectionVersion);
            application.SaveResidenceSection(applicantUserId, now, allowInvalid);
        }, cancellationToken);
    }

    public async Task<ServiceResult> AddCoApplicantAsync(
        int applicationId, string actingUserId, string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var coApplicantId = await (
                from user in _db.Users
                join userRole in _db.UserRoles on user.Id equals userRole.UserId
                join role in _db.Roles on userRole.RoleId equals role.Id
                where user.NormalizedEmail == normalizedEmail && role.Name == Roles.Applicant
                select user.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (coApplicantId is null)
            return ServiceResult.Failure(NoApplicantAccountMessage);

        return await _updater.ApplyAsync(
            applicationId, (application, _) => application.AddApplicant(actingUserId, coApplicantId), cancellationToken);
    }

    private static void EnsureResidenceSectionVersion(RentalApplication application, Guid expected)
    {
        if (application.ResidenceSectionVersion != expected)
            throw new StaleDataException();
    }

    private static ResidenceDetails ToResidenceDetails(ResidenceInput input)
    {
        return new ResidenceDetails
        {
            Address = new Address(input.Street, input.City, input.State, input.PostalCode),
            LandlordName = input.LandlordName,
            LandlordPhone = input.LandlordPhone,
            MoveInDate = input.MoveInDate,
            MoveOutDate = input.MoveOutDate
        };
    }
}
