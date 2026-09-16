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
                select new ApplicantListItemViewModel(
                    user.FirstName + " " + user.LastName,
                    user.Email ?? string.Empty,
                    applicant.IsPrimary,
                    applicant.DetailsSavedAt != null))
            .ToListAsync(HttpContext.RequestAborted);

        return View(new ApplicantListViewModel(applicationId, editable, people));
    }
}
