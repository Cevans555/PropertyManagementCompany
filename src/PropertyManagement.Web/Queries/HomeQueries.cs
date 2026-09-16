using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Enums;
using PropertyManagement.Core.Security;
using PropertyManagement.Data;
using PropertyManagement.Web.Infrastructure;
using PropertyManagement.Web.Models.Home;

namespace PropertyManagement.Web.Queries;

public sealed class HomeQueries
{
    private readonly PropertyManagementDbContext _db;
    private readonly TimeProvider _timeProvider;

    public HomeQueries(PropertyManagementDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<HomePageViewModel> DashboardAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (user.Identity?.IsAuthenticated != true)
            return new HomePageViewModel { IsAuthenticated = false, IsManager = false };

        return user.IsInRole(Roles.PropertyManager)
            ? await ManagerDashboardAsync(user, cancellationToken)
            : await ApplicantDashboardAsync(user, cancellationToken);
    }

    private async Task<HomePageViewModel> ManagerDashboardAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var managerId = user.Id();
        var today = _timeProvider.Today();

        return new HomePageViewModel
        {
            IsAuthenticated = true,
            IsManager = true,
            DisplayName = user.Identity!.Name,
            WaitingToReview = await _db.RentalApplications
                .CountAsync(a => a.Status == ApplicationStatus.Submitted, cancellationToken),
            ClaimedByMe = await _db.RentalApplications
                .CountAsync(a => a.Status == ApplicationStatus.UnderReview && a.ClaimedById == managerId, cancellationToken),
            PropertyCount = await _db.Properties.CountAsync(cancellationToken),
            AvailableUnitCount = await _db.Units
                .CountAsync(u => !u.Leases.Any(l => l.StartDate <= today && l.EndDate >= today), cancellationToken)
        };
    }

    private async Task<HomePageViewModel> ApplicantDashboardAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var userId = user.Id();
        var mine = _db.RentalApplications.Where(a => a.Applicants.Any(p => p.UserId == userId));

        return new HomePageViewModel
        {
            IsAuthenticated = true,
            IsManager = false,
            DisplayName = user.Identity!.Name,
            NeedsAttention = await mine
                .CountAsync(a => a.Status == ApplicationStatus.Draft || a.Status == ApplicationStatus.Returned, cancellationToken),
            AwaitingDecision = await mine
                .CountAsync(a => a.Status == ApplicationStatus.Submitted || a.Status == ApplicationStatus.UnderReview, cancellationToken),
            Approved = await mine.CountAsync(a => a.Status == ApplicationStatus.Approved, cancellationToken)
        };
    }
}
