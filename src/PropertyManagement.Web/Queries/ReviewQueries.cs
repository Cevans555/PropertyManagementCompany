using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Enums;
using PropertyManagement.Data;
using PropertyManagement.Web.Models.Reviews;

namespace PropertyManagement.Web.Queries;

public sealed class ReviewQueries
{
    private readonly PropertyManagementDbContext _db;

    public ReviewQueries(PropertyManagementDbContext db)
    {
        _db = db;
    }

    public async Task<ReviewQueueViewModel> QueueAsync(string? managerId, CancellationToken cancellationToken = default)
    {
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
            .ToListAsync(cancellationToken);

        return new ReviewQueueViewModel
        {
            Waiting = rows.Where(r => r.Status == ApplicationStatus.Submitted).Select(r => r.Row).ToList(),
            MyClaims = rows.Where(r => r.Status == ApplicationStatus.UnderReview && r.ClaimedById == managerId).Select(r => r.Row).ToList(),
            OtherClaims = rows.Where(r => r.Status == ApplicationStatus.UnderReview && r.ClaimedById != managerId).Select(r => r.Row).ToList()
        };
    }

    public async Task<IReadOnlyList<ManagerNoteRowViewModel>> NotesAsync(
        int applicationId, CancellationToken cancellationToken = default)
    {
        return await _db.ManagerNotes
            .AsNoTracking()
            .Where(n => n.RentalApplicationId == applicationId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new ManagerNoteRowViewModel
            {
                Id = n.Id,
                Text = n.Text,
                Author = _db.Users.Where(u => u.Id == n.CreatedById).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault() ?? n.CreatedById,
                CreatedAt = n.CreatedAt,
                ModifiedAt = n.ModifiedAt,
                ModifiedBy = _db.Users.Where(u => u.Id == n.ModifiedById).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }
}
