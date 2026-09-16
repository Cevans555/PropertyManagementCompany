using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Common;
using PropertyManagement.Data;
using PropertyManagement.Web.Models.Properties;

namespace PropertyManagement.Web.ViewComponents;

public class UnitTableViewComponent : ViewComponent
{
    private readonly PropertyManagementDbContext _db;
    private readonly TimeProvider _timeProvider;

    public UnitTableViewComponent(PropertyManagementDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<IViewComponentResult> InvokeAsync(int propertyId)
    {
        var today = _timeProvider.Today();

        var units = await _db.Units
            .AsNoTracking()
            .Where(u => u.PropertyId == propertyId)
            .OrderBy(u => u.UnitNumber)
            .Select(u => new UnitRowViewModel(
                u.Id,
                u.UnitNumber,
                u.UnitType.Name,
                u.UnitType.IsActive,
                u.Bedrooms,
                u.MonthlyRent,
                u.Leases
                    .Where(l => l.StartDate <= today && l.EndDate >= today)
                    .Select(l => (DateOnly?)l.EndDate)
                    .FirstOrDefault(),
                _db.RentalApplications.Count(a => a.UnitId == u.Id)))
            .ToListAsync(HttpContext.RequestAborted);

        return View(new UnitTableViewModel(propertyId, units));
    }
}
