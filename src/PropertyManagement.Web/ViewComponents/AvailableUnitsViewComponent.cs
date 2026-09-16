using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Enums;
using PropertyManagement.Data;
using PropertyManagement.Web.Models.Applications;

namespace PropertyManagement.Web.ViewComponents;

public class AvailableUnitsViewComponent : ViewComponent
{
    private readonly PropertyManagementDbContext _db;
    private readonly TimeProvider _timeProvider;

    public AvailableUnitsViewComponent(PropertyManagementDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var today = _timeProvider.Today();
        var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        var units = await _db.Units
            .AsNoTracking()
            .Where(u => !u.Leases.Any(l => l.StartDate <= today && l.EndDate >= today))
            .OrderBy(u => u.Property.Name)
            .ThenBy(u => u.UnitNumber)
            .Select(u => new AvailableUnitViewModel(
                u.Id,
                u.Property.Name,
                u.Property.Address.City,
                u.Property.Address.State,
                u.UnitNumber,
                u.UnitType.Name,
                u.Bedrooms,
                u.MonthlyRent,
                _db.RentalApplications
                    .Where(a => a.UnitId == u.Id
                        && a.Applicants.Any(p => p.UserId == userId)
                        && a.Status != ApplicationStatus.Approved
                        && a.Status != ApplicationStatus.Denied
                        && a.Status != ApplicationStatus.Withdrawn)
                    .Select(a => (int?)a.Id)
                    .FirstOrDefault()))
            .ToListAsync(HttpContext.RequestAborted);

        return View(units);
    }
}
