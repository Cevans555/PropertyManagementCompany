using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Entities;
using PropertyManagement.Data;
using PropertyManagement.Web.Authorization;
using PropertyManagement.Web.Models.Applications;
using PropertyManagement.Web.Models.Applications.Forms;
using PropertyManagement.Web.Models.Applications.Page;
using PropertyManagement.Web.Queries;

namespace PropertyManagement.Web.Services;

public sealed class ApplicationPageBuilder
{
    private readonly PropertyManagementDbContext _db;
    private readonly IAuthorizationService _authorizationService;
    private readonly TimeProvider _timeProvider;
    private readonly ExistingLeaseQueries _existingLeases;

    public ApplicationPageBuilder(
        PropertyManagementDbContext db,
        IAuthorizationService authorizationService,
        TimeProvider timeProvider,
        ExistingLeaseQueries existingLeases)
    {
        _db = db;
        _authorizationService = authorizationService;
        _timeProvider = timeProvider;
        _existingLeases = existingLeases;
    }

    public Task<RentalApplication?> LoadAsync(int applicationId, CancellationToken cancellationToken)
    {
        return _db.RentalApplications
            .AsNoTracking()
            .Include(a => a.Applicants)
            .Include(a => a.Residences)
            .Include(a => a.Unit).ThenInclude(u => u.Property)
            .Include(a => a.Unit).ThenInclude(u => u.UnitType)
            .AsSplitQuery()
            .SingleOrDefaultAsync(a => a.Id == applicationId, cancellationToken);
    }

    public async Task<bool> IsAllowedAsync(
        ClaimsPrincipal user, RentalApplication application, OperationAuthorizationRequirement operation)
    {
        var result = await _authorizationService.AuthorizeAsync(user, application, operation);
        return result.Succeeded;
    }

    public async Task<ApplicationAccess> AuthorizeAsync(
        ClaimsPrincipal user,
        int applicationId,
        OperationAuthorizationRequirement operation,
        CancellationToken cancellationToken)
    {
        var application = await LoadAsync(applicationId, cancellationToken);
        if (application is null || !await IsAllowedAsync(user, application, ApplicationOperations.View))
            return ApplicationAccess.NotFound();

        if (operation != ApplicationOperations.View && !await IsAllowedAsync(user, application, operation))
            return ApplicationAccess.Forbidden();

        return ApplicationAccess.Allowed(application);
    }

    public async Task<ApplicationPageViewModel> BuildAsync(
        RentalApplication application,
        ApplicationSection section,
        ClaimsPrincipal user,
        ApplicationPageViewModel? posted,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        var applicantUserIds = application.Applicants.Select(a => a.UserId).ToList();
        var accounts = await _db.Users
            .AsNoTracking()
            .Where(u => applicantUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var people = application.Applicants
            .OrderByDescending(a => a.IsPrimary)
            .ThenBy(a => a.Id)
            .Select(a =>
            {
                var account = accounts.GetValueOrDefault(a.UserId);
                return new ApplicantPersonViewModel
                {
                    UserId = a.UserId,
                    AccountName = account is null ? "Unknown user" : $"{account.FirstName} {account.LastName}".Trim(),
                    AccountEmail = account?.Email ?? string.Empty,
                    IsPrimary = a.IsPrimary,
                    IsCurrentUser = a.UserId == userId,
                    HasSavedDetails = a.HasSavedDetails,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    Phone = a.Phone,
                    Email = a.Email,
                    CurrentAddress = AddressText(a)
                };
            })
            .ToList();

        var currentApplicant = application.Applicants.SingleOrDefault(a => a.UserId == userId);
        var currentAccount = userId is null ? null : accounts.GetValueOrDefault(userId);
        var applicantDetails = posted?.ApplicantDetails ?? new ApplicantDetailsFormModel
        {
            FirstName = currentApplicant?.FirstName ?? currentAccount?.FirstName ?? string.Empty,
            LastName = currentApplicant?.LastName ?? currentAccount?.LastName ?? string.Empty,
            Phone = currentApplicant?.Phone ?? string.Empty,
            Email = currentApplicant?.Email ?? currentAccount?.Email ?? string.Empty,
            Street = currentApplicant?.Street ?? string.Empty,
            City = currentApplicant?.City ?? string.Empty,
            State = currentApplicant?.State ?? string.Empty,
            PostalCode = currentApplicant?.PostalCode ?? string.Empty
        };

        var canReview = await IsAllowedAsync(user, application, ApplicationOperations.Review);
        var claimedByName = application.ClaimedById is null
            ? null
            : await _db.Users
                .Where(u => u.Id == application.ClaimedById)
                .Select(u => u.FirstName + " " + u.LastName)
                .SingleOrDefaultAsync(cancellationToken);

        return new ApplicationPageViewModel
        {
            Id = application.Id,
            Section = section,
            ApplicantDetails = applicantDetails,
            ApplicantRowVersion = posted?.ApplicantRowVersion
                ?? (currentApplicant is null ? null : Convert.ToBase64String(currentApplicant.RowVersion)),
            ResidenceSectionVersion = application.ResidenceSectionVersion,
            Header = new ApplicationHeaderViewModel
            {
                Id = application.Id,
                Status = application.Status,
                PropertyName = application.Unit.Property.Name,
                UnitNumber = application.Unit.UnitNumber,
                UnitTypeName = application.Unit.UnitType.Name,
                Bedrooms = application.Unit.Bedrooms,
                MonthlyRent = application.Unit.MonthlyRent,
                CreatedAt = application.CreatedAt,
                SubmittedAt = application.SubmittedAt
            },
            CanEdit = await IsAllowedAsync(user, application, ApplicationOperations.Edit),
            CanWithdraw = await IsAllowedAsync(user, application, ApplicationOperations.Withdraw),
            CanSeeStatusHistory = canReview,
            CanReview = canReview,
            CanManageNotes = await IsAllowedAsync(user, application, ApplicationOperations.ManageNotes),
            ClaimedByName = claimedByName,
            IsClaimedByCurrentUser = userId is not null && application.ClaimedById == userId,
            IsApplicantOnApplication = currentApplicant is not null,
            People = people,
            StatusHistory = canReview ? await StatusHistoryAsync(application.Id, cancellationToken) : [],
            ExistingLeases = canReview ? await _existingLeases.ForApplicationAsync(application.Id, cancellationToken) : [],
            SavedApplicantDetailsErrors = currentApplicant is null ? [] : currentApplicant.GetDetailsErrors(),
            SavedResidenceErrors = application.GetResidenceSectionErrors().Select(e => e.Message).ToList(),
            SubmitBlockers = await SubmitBlockersAsync(application, people, cancellationToken)
        };
    }

    private Task<List<StatusHistoryRowViewModel>> StatusHistoryAsync(int applicationId, CancellationToken cancellationToken)
    {
        return (from history in _db.StatusHistories.AsNoTracking()
                where history.RentalApplicationId == applicationId
                join changedBy in _db.Users on history.ChangedById equals changedBy.Id into changedByUsers
                from changedBy in changedByUsers.DefaultIfEmpty()
                orderby history.ChangedAt, history.Id
                select new StatusHistoryRowViewModel
                {
                    FromStatus = history.FromStatus,
                    ToStatus = history.ToStatus,
                    ChangedBy = changedBy == null ? history.ChangedById : changedBy.FirstName + " " + changedBy.LastName,
                    ChangedAt = history.ChangedAt,
                    Comment = history.Comment
                })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<SubmitBlocker>> SubmitBlockersAsync(
        RentalApplication application, IReadOnlyList<ApplicantPersonViewModel> people, CancellationToken cancellationToken)
    {
        if (!application.IsEditable)
            return [];

        var blockers = new List<SubmitBlocker>();
        foreach (var person in people)
        {
            var applicant = application.Applicants.Single(a => a.UserId == person.UserId);
            if (!applicant.HasSavedDetails)
            {
                blockers.Add(person.IsCurrentUser
                    ? new SubmitBlocker { Message = "Save your applicant information.", FixSection = ApplicationSection.Applicant }
                    : new SubmitBlocker { Message = $"{person.AccountName} still needs to save their applicant information." });
                continue;
            }

            foreach (var error in applicant.GetDetailsErrors())
            {
                blockers.Add(person.IsCurrentUser
                    ? new SubmitBlocker { Message = $"Applicant information: {error.Message}", FixSection = ApplicationSection.Applicant }
                    : new SubmitBlocker { Message = $"{person.AccountName}'s applicant information: {error.Message}" });
            }
        }

        if (application.ResidenceSectionSavedAt is null)
        {
            blockers.Add(new SubmitBlocker
            {
                Message = "Save your residence history with at least one prior residence.",
                FixSection = ApplicationSection.Residences
            });
        }
        else
        {
            foreach (var error in application.GetResidenceSectionErrors())
            {
                blockers.Add(new SubmitBlocker
                {
                    Message = $"Residence history: {error.Message}",
                    FixSection = ApplicationSection.Residences
                });
            }
        }

        var today = _timeProvider.Today();
        var leased = await _db.Leases.AnyAsync(
            l => l.UnitId == application.UnitId && l.StartDate <= today && l.EndDate >= today, cancellationToken);
        if (leased)
            blockers.Add(new SubmitBlocker { Message = "This unit has been leased, so the application can't be submitted." });

        return blockers;
    }

    private static string? AddressText(Applicant applicant)
    {
        var stateLine = string.Join(" ", new[] { applicant.State, applicant.PostalCode }.Where(v => !string.IsNullOrWhiteSpace(v)));
        var text = string.Join(", ", new[] { applicant.Street, applicant.City, stateLine }.Where(v => !string.IsNullOrWhiteSpace(v)));
        return text.Length == 0 ? null : text;
    }
}
