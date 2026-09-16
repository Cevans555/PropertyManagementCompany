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
                Row = new QueueRowViewModel
                {
                    ApplicationId = a.Id,
                    PropertyName = a.Unit.Property.Name,
                    UnitNumber = a.Unit.UnitNumber,
                    ApplicantName = _db.Users
                        .Where(u => u.Id == a.Applicants.Where(p => p.IsPrimary).Select(p => p.UserId).FirstOrDefault())
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefault() ?? string.Empty,
                    SubmittedAt = a.SubmittedAt,
                    ClaimedBy = _db.Users.Where(u => u.Id == a.ClaimedById).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
                    ClaimedAt = a.ClaimedAt
                }
            })
            .ToListAsync(HttpContext.RequestAborted);

        return View(new ReviewQueueViewModel
        {
            Waiting = rows.Where(r => r.Status == ApplicationStatus.Submitted).Select(r => r.Row).ToList(),
            MyClaims = rows.Where(r => r.Status == ApplicationStatus.UnderReview && r.ClaimedById == managerId).Select(r => r.Row).ToList(),
            OtherClaims = rows.Where(r => r.Status == ApplicationStatus.UnderReview && r.ClaimedById != managerId).Select(r => r.Row).ToList()
        });
    }
}
