using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Enums;
using PropertyManagement.Data;
using PropertyManagement.Web.Models.Reviews;

namespace PropertyManagement.Web.ViewComponents;

public class ReviewQueueViewComponent : ViewComponent
{
    private readonly PropertyManagementDbContext _db;

    public ReviewQueueViewComponent(PropertyManagementDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var managerId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        var rows = await _db.RentalApplications
            .AsNoTracking()
            .Where(a => a.Status == ApplicationStatus.Submitted || a.Status == ApplicationStatus.UnderReview)
            .OrderBy(a => a.SubmittedAt)
            .Select(a => new
            {
                a.Status,
                a.ClaimedById,
                Row = new QueueRowViewModel(
                    a.Id,
                    a.Unit.Property.Name,
                    a.Unit.UnitNumber,
                    _db.Users
                        .Where(u => u.Id == a.Applicants.Where(p => p.IsPrimary).Select(p => p.UserId).FirstOrDefault())
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefault() ?? string.Empty,
                    a.SubmittedAt,
                    _db.Users.Where(u => u.Id == a.ClaimedById).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
                    a.ClaimedAt)
            })
            .ToListAsync(HttpContext.RequestAborted);

        return View(new ReviewQueueViewModel(
            rows.Where(r => r.Status == ApplicationStatus.Submitted).Select(r => r.Row).ToList(),
            rows.Where(r => r.Status == ApplicationStatus.UnderReview && r.ClaimedById == managerId).Select(r => r.Row).ToList(),
            rows.Where(r => r.Status == ApplicationStatus.UnderReview && r.ClaimedById != managerId).Select(r => r.Row).ToList()));
    }
}
