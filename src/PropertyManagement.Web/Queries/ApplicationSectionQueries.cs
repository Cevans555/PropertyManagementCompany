using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Web.Models.Applications;

namespace PropertyManagement.Web.Queries;

public sealed class ApplicationSectionQueries
{
    private readonly PropertyManagementDbContext _db;

    public ApplicationSectionQueries(PropertyManagementDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ApplicantListItemViewModel>> ApplicantsAsync(
        int applicationId, CancellationToken cancellationToken = default)
    {
        return await (
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
            .ToListAsync(cancellationToken);
    }

    public Task<Guid> ResidenceSectionVersionAsync(int applicationId, CancellationToken cancellationToken = default)
    {
        return _db.RentalApplications
            .Where(a => a.Id == applicationId)
            .Select(a => a.ResidenceSectionVersion)
            .SingleAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResidenceRowViewModel>> ResidencesAsync(
        int applicationId, CancellationToken cancellationToken = default)
    {
        return await _db.ResidenceHistories
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
            .ToListAsync(cancellationToken);
    }
}
