using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Security;
using PropertyManagement.Data;
using PropertyManagement.Web.Models.Applications;
using PropertyManagement.Web.Models.Grid;

namespace PropertyManagement.Web.Services;

public sealed class ApplicationListQuery
{
    private readonly PropertyManagementDbContext _db;

    public ApplicationListQuery(PropertyManagementDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ApplicationListRowViewModel>> ListAsync(
        ClaimsPrincipal user, ApplicationListFilter filter, ApplicationListPaging paging, CancellationToken cancellationToken)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var isManager = user.IsInRole(Roles.PropertyManager);
        var query = _db.RentalApplications.AsNoTracking();

        if (!isManager)
            query = query.Where(a => a.Applicants.Any(p => p.UserId == userId));

        if (filter.Status is { } status)
            query = query.Where(a => a.Status == status);

        if (filter.PropertyId is { } propertyId)
            query = query.Where(a => a.Unit.PropertyId == propertyId);

        var totalCount = await query.CountAsync(cancellationToken);
        var page = PageMath.ClampPage(paging.Page, totalCount, paging.PageSize);

        var sort = paging.Sort;
        if (sort == ApplicationSortField.ClaimedBy && !isManager)
            sort = ApplicationSortField.Submitted;

        var rows = await Sort(query, sort, paging.Direction)
            .Skip((page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .Select(a => new
            {
                a.Id,
                PropertyName = a.Unit.Property.Name,
                a.Unit.UnitNumber,
                a.Status,
                a.CreatedAt,
                a.SubmittedAt,
                ApplicantUserIds = a.Applicants.OrderByDescending(p => p.IsPrimary).ThenBy(p => p.Id).Select(p => p.UserId).ToList(),
                ClaimedBy = isManager
                    ? _db.Users.Where(u => u.Id == a.ClaimedById).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault()
                    : null
            })
            .ToListAsync(cancellationToken);

        var userIds = rows.SelectMany(r => r.ApplicantUserIds).Distinct().ToList();
        var names = await _db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var items = rows
            .Select(r => new ApplicationListRowViewModel
            {
                Id = r.Id,
                PropertyName = r.PropertyName,
                UnitNumber = r.UnitNumber,
                Status = r.Status,
                Applicants = string.Join(", ", r.ApplicantUserIds.Select(id => names.GetValueOrDefault(id, "Unknown user"))),
                CreatedAt = r.CreatedAt,
                SubmittedAt = r.SubmittedAt,
                ClaimedBy = r.ClaimedBy
            })
            .ToList();

        return new PagedResult<ApplicationListRowViewModel>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = paging.PageSize
        };
    }

    private IOrderedQueryable<RentalApplication> Sort(
        IQueryable<RentalApplication> query, ApplicationSortField sort, SortDirection direction)
    {
        var descending = direction == SortDirection.Desc;

        IOrderedQueryable<RentalApplication> ordered;
        switch (sort)
        {
            case ApplicationSortField.Property:
                ordered = query
                    .OrderBy(a => a.Unit.Property.Name, descending)
                    .ThenBy(a => a.Unit.UnitNumber, descending);
                break;
            case ApplicationSortField.Status:
                ordered = query.OrderBy(a => a.Status, descending);
                break;
            case ApplicationSortField.ClaimedBy:
                ordered = query.OrderBy(
                    a => _db.Users.Where(u => u.Id == a.ClaimedById).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
                    descending);
                break;
            default:
                ordered = query.OrderBy(a => a.SubmittedAt ?? a.CreatedAt, descending);
                break;
        }

        return ordered.ThenBy(a => a.Id, descending);
    }
}
