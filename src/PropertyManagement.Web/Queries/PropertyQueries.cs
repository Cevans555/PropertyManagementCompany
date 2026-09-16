using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Common;
using PropertyManagement.Data;
using PropertyManagement.Web.Models.Properties;

namespace PropertyManagement.Web.Queries;

public sealed class PropertyQueries
{
    private readonly PropertyManagementDbContext _db;
    private readonly TimeProvider _timeProvider;

    public PropertyQueries(PropertyManagementDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<PropertyListItemViewModel>> ListAsync(CancellationToken cancellationToken = default)
    {
        var today = _timeProvider.Today();

        return await _db.Properties
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new PropertyListItemViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Street = p.Address.Street,
                City = p.Address.City,
                State = p.Address.State,
                PostalCode = p.Address.PostalCode,
                UnitCount = p.Units.Count(),
                AvailableUnitCount = p.Units.Count(u => !u.Leases.Any(l => l.StartDate <= today && l.EndDate >= today))
            })
            .ToListAsync(cancellationToken);
    }

    public Task<PropertyHeaderViewModel?> HeaderAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return _db.Properties
            .AsNoTracking()
            .Where(p => p.Id == propertyId)
            .Select(p => new PropertyHeaderViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Street = p.Address.Street,
                City = p.Address.City,
                State = p.Address.State,
                PostalCode = p.Address.PostalCode
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<PropertyFormViewModel?> FormAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return _db.Properties
            .AsNoTracking()
            .Where(p => p.Id == propertyId)
            .Select(p => new PropertyFormViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Street = p.Address.Street,
                City = p.Address.City,
                State = p.Address.State,
                PostalCode = p.Address.PostalCode
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<PropertyDeleteInfo?> DeleteInfoAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return _db.Properties
            .AsNoTracking()
            .Where(p => p.Id == propertyId)
            .Select(p => new PropertyDeleteInfo(p.Name, p.Units.Count()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<string?> NameAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return _db.Properties
            .Where(p => p.Id == propertyId)
            .Select(p => p.Name)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SelectListItem>> OptionsAsync(int? selectedId, CancellationToken cancellationToken = default)
    {
        var properties = await _db.Properties
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(cancellationToken);

        return properties
            .Select(p => new SelectListItem(p.Name, p.Id.ToString(CultureInfo.InvariantCulture), p.Id == selectedId))
            .ToList();
    }
}
