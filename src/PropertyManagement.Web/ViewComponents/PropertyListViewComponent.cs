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
            .Select(p => new PropertyListItemViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Street = p.Address.Street,
                City = p.Address.City,
                State = p.Address.State,
                PostalCode = p.Address.PostalCode,
                UnitCount = p.Units.Count(),
                AvailableUnitCount = p.Units.Count(u => !u.Leases.Any(l => l.StartDate <= today && l.EndDate >= today))
            })
            .ToListAsync(HttpContext.RequestAborted);

        return View(properties);
    }
}
