using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Web.Models.Applications;

namespace PropertyManagement.Web.ViewComponents;

public class ResidenceListViewComponent : ViewComponent
{
    private readonly PropertyManagementDbContext _db;

    public ResidenceListViewComponent(PropertyManagementDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync(int applicationId, bool editable)
    {
        var sectionVersion = await _db.RentalApplications
            .Where(a => a.Id == applicationId)
            .Select(a => a.ResidenceSectionVersion)
            .SingleAsync(HttpContext.RequestAborted);

        var residences = await _db.ResidenceHistories
            .AsNoTracking()
            .Where(r => r.RentalApplicationId == applicationId)
            .OrderByDescending(r => r.MoveOutDate)
            .Select(r => new ResidenceRowViewModel
            {
                Id = r.Id,
                Address = r.Address.Street + ", " + r.Address.City + ", " + r.Address.State + " " + r.Address.PostalCode,
                LandlordName = r.LandlordName,
                LandlordPhone = r.LandlordPhone,
                MoveInDate = r.MoveInDate,
                MoveOutDate = r.MoveOutDate
            })
            .ToListAsync(HttpContext.RequestAborted);

        return View(new ResidenceListViewModel
        {
            ApplicationId = applicationId,
            Editable = editable,
            SectionVersion = sectionVersion,
            Residences = residences
        });
    }
}
