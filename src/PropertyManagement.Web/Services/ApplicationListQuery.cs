using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Enums;
using PropertyManagement.Core.Security;
using PropertyManagement.Data;
using PropertyManagement.Web.Models.Applications;

namespace PropertyManagement.Web.Services;

public sealed class ApplicationListQuery
{
    private readonly PropertyManagementDbContext _db;

    public ApplicationListQuery(PropertyManagementDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ApplicationListRowViewModel>> ListAsync(
        ClaimsPrincipal user, ApplicationListFilter filter, CancellationToken cancellationToken)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = _db.RentalApplications.AsNoTracking();

        if (!user.IsInRole(Roles.PropertyManager))
            query = query.Where(a => a.Applicants.Any(p => p.UserId == userId));

        if (filter.Status is { } status)
            query = query.Where(a => a.Status == status);

        if (filter.PropertyId is { } propertyId)
            query = query.Where(a => a.Unit.PropertyId == propertyId);

        var rows = await query
            .OrderByDescending(a => a.SubmittedAt ?? a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Select(a => new
            {
                a.Id,
                PropertyName = a.Unit.Property.Name,
                a.Unit.UnitNumber,
                a.Status,
                a.CreatedAt,
                a.SubmittedAt,
                ApplicantUserIds = a.Applicants.OrderByDescending(p => p.IsPrimary).ThenBy(p => p.Id).Select(p => p.UserId).ToList(),
                ClaimedBy = _db.Users.Where(u => u.Id == a.ClaimedById).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var userIds = rows.SelectMany(r => r.ApplicantUserIds).Distinct().ToList();
        var names = await _db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        return rows
            .Select(r => new ApplicationListRowViewModel(
                r.Id,
                r.PropertyName,
                r.UnitNumber,
                r.Status,
                string.Join(", ", r.ApplicantUserIds.Select(id => names.GetValueOrDefault(id, "Unknown user"))),
                r.CreatedAt,
                r.SubmittedAt,
                r.ClaimedBy))
            .ToList();
    }
}
