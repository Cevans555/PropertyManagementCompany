using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Web.Models.Applications;

namespace PropertyManagement.Web.ViewComponents;

public class ApplicantListViewComponent : ViewComponent
{
    private readonly PropertyManagementDbContext _db;

    public ApplicantListViewComponent(PropertyManagementDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync(int applicationId, bool editable)
    {
        var people = await (
                from applicant in _db.Applicants.AsNoTracking()
                where applicant.RentalApplicationId == applicationId
                join user in _db.Users on applicant.UserId equals user.Id
                orderby applicant.IsPrimary descending, applicant.Id
                select new ApplicantListItemViewModel
                {
                    Name = user.FirstName + " " + user.LastName,
                    Email = user.Email ?? string.Empty,
                    IsPrimary = applicant.IsPrimary,
                    HasSavedDetails = applicant.DetailsSavedAt != null
                })
            .ToListAsync(HttpContext.RequestAborted);

        return View(new ApplicantListViewModel
        {
            ApplicationId = applicationId,
            Editable = editable,
            People = people
        });
    }
}
