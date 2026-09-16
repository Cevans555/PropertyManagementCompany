using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Enums;
using PropertyManagement.Data;
using PropertyManagement.Web.Models.Applications;
using PropertyManagement.Web.Models.Units;

namespace PropertyManagement.Web.Queries;

public sealed class UnitQueries
{
    private readonly PropertyManagementDbContext _db;
    private readonly TimeProvider _timeProvider;

    public UnitQueries(PropertyManagementDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<UnitRowViewModel>> TableAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var today = _timeProvider.Today();

        return await _db.Units
            .AsNoTracking()
            .Where(u => u.PropertyId == propertyId)
            .OrderBy(u => u.UnitNumber)
            .Select(u => new UnitRowViewModel
            {
                Id = u.Id,
                UnitNumber = u.UnitNumber,
                UnitTypeName = u.UnitType.Name,
                UnitTypeIsActive = u.UnitType.IsActive,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent,
                LeasedUntil = u.Leases
                    .Where(l => l.StartDate <= today && l.EndDate >= today)
                    .Select(l => (DateOnly?)l.EndDate)
                    .FirstOrDefault(),
                ApplicationCount = _db.RentalApplications.Count(a => a.UnitId == u.Id)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AvailableUnitViewModel>> AvailableAsync(
        string? userId, CancellationToken cancellationToken = default)
    {
        var today = _timeProvider.Today();

        return await _db.Units
            .AsNoTracking()
            .Where(u => !u.Leases.Any(l => l.StartDate <= today && l.EndDate >= today))
            .OrderBy(u => u.Property.Name)
            .ThenBy(u => u.UnitNumber)
            .Select(u => new AvailableUnitViewModel
            {
                UnitId = u.Id,
                PropertyName = u.Property.Name,
                City = u.Property.Address.City,
                State = u.Property.Address.State,
                UnitNumber = u.UnitNumber,
                UnitTypeName = u.UnitType.Name,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent,
                OpenApplicationId = _db.RentalApplications
                    .Where(a => a.UnitId == u.Id
                        && a.Applicants.Any(p => p.UserId == userId)
                        && a.Status != ApplicationStatus.Approved
                        && a.Status != ApplicationStatus.Denied
                        && a.Status != ApplicationStatus.Withdrawn)
                    .Select(a => (int?)a.Id)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }

    public Task<UnitFormViewModel?> FormAsync(int unitId, CancellationToken cancellationToken = default)
    {
        return _db.Units
            .AsNoTracking()
            .Where(u => u.Id == unitId)
            .Select(u => new UnitFormViewModel
            {
                Id = u.Id,
                PropertyId = u.PropertyId,
                PropertyName = u.Property.Name,
                UnitNumber = u.UnitNumber,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent,
                UnitTypeId = u.UnitTypeId
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<UnitFormContext?> FormContextAsync(int unitId, CancellationToken cancellationToken = default)
    {
        return _db.Units
            .AsNoTracking()
            .Where(u => u.Id == unitId)
            .Select(u => new UnitFormContext(u.PropertyId, u.Property.Name, u.UnitTypeId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<UnitDeleteInfo?> DeleteInfoAsync(int unitId, CancellationToken cancellationToken = default)
    {
        return _db.Units
            .AsNoTracking()
            .Where(u => u.Id == unitId)
            .Select(u => new UnitDeleteInfo(u.UnitNumber, u.Property.Name))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SelectListItem>> TypeOptionsAsync(
        int? currentUnitTypeId, CancellationToken cancellationToken = default)
    {
        var unitTypes = await _db.UnitTypes
            .AsNoTracking()
            .Where(t => t.IsActive || t.Id == currentUnitTypeId)
            .OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name, t.IsActive })
            .ToListAsync(cancellationToken);

        return unitTypes
            .Select(t => new SelectListItem(
                t.IsActive ? t.Name : $"{t.Name} (inactive)",
                t.Id.ToString(CultureInfo.InvariantCulture)))
            .ToList();
    }
}
