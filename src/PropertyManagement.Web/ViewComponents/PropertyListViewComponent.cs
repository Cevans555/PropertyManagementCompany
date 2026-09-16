using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Common;
using PropertyManagement.Data;
using PropertyManagement.Web.Models.Properties;

namespace PropertyManagement.Web.ViewComponents;

public class PropertyListViewComponent : ViewComponent
{
    private readonly PropertyManagementDbContext _db;
    private readonly TimeProvider _timeProvider;

    public PropertyListViewComponent(PropertyManagementDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var today = _timeProvider.Today();

        var properties = await _db.Properties
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new PropertyListItemViewModel(
                p.Id,
                p.Name,
                p.Address.Street,
                p.Address.City,
                p.Address.State,
                p.Address.PostalCode,
                p.Units.Count(),
                p.Units.Count(u => !u.Leases.Any(l => l.StartDate <= today && l.EndDate >= today))))
            .ToListAsync(HttpContext.RequestAborted);

        return View(properties);
    }
}
