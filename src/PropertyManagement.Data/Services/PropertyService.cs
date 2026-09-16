using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Dtos;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.ValueObjects;

namespace PropertyManagement.Data.Services;

public sealed class PropertyService
{
    public const string PropertyNotFoundMessage = "Property not found.";
    public const string UnitNotFoundMessage = "Unit not found.";
    public const string UnitTypeNotFoundMessage = "Choose a unit type.";
    public const string DuplicateUnitNumberMessage = "Another unit at this property already uses that unit number.";
    public const string PropertyInUseMessage = "This property has units with applications or leases, so it can't be removed.";
    public const string UnitInUseMessage = "This unit has applications or leases, so it can't be removed.";

    private const int SqlUniqueIndexViolation = 2601;
    private const int SqlUniqueConstraintViolation = 2627;

    private readonly PropertyManagementDbContext _db;

    public PropertyService(PropertyManagementDbContext db)
    {
        _db = db;
    }

    public Task<ServiceResult<int>> CreatePropertyAsync(PropertyDetails details, CancellationToken cancellationToken = default)
    {
        return RunAsync(async () =>
        {
            var property = new Property(details.Name, ToAddress(details));
            _db.Properties.Add(property);
            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult<int>.Success(property.Id);
        }, ServiceResult<int>.Failure);
    }

    public Task<ServiceResult> UpdatePropertyAsync(int propertyId, PropertyDetails details, CancellationToken cancellationToken = default)
    {
        return RunAsync(async () =>
        {
            var property = await _db.Properties.SingleOrDefaultAsync(p => p.Id == propertyId, cancellationToken);
            if (property is null)
                return ServiceResult.Failure(PropertyNotFoundMessage);

            property.Update(details.Name, ToAddress(details));
            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult.Success();
        }, ServiceResult.Failure);
    }

    public Task<ServiceResult> DeletePropertyAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return RunAsync(async () =>
        {
            var property = await _db.Properties
                .Include(p => p.Units)
                .SingleOrDefaultAsync(p => p.Id == propertyId, cancellationToken);
            if (property is null)
                return ServiceResult.Failure(PropertyNotFoundMessage);

            var unitIds = property.Units.Select(u => u.Id).ToList();
            if (await AnyUnitInUseAsync(unitIds, cancellationToken))
                return ServiceResult.Failure(PropertyInUseMessage);

            _db.Properties.Remove(property);
            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult.Success();
        }, ServiceResult.Failure);
    }

    public Task<ServiceResult<int>> AddUnitAsync(int propertyId, UnitDetails details, CancellationToken cancellationToken = default)
    {
        return RunAsync(async () =>
        {
            var property = await _db.Properties
                .Include(p => p.Units)
                .SingleOrDefaultAsync(p => p.Id == propertyId, cancellationToken);
            if (property is null)
                return ServiceResult<int>.Failure(PropertyNotFoundMessage);

            var unitType = await _db.UnitTypes.SingleOrDefaultAsync(t => t.Id == details.UnitTypeId, cancellationToken);
            if (unitType is null)
                return ServiceResult<int>.Failure(UnitTypeNotFoundMessage);

            var unit = property.AddUnit(details.UnitNumber, details.Bedrooms, details.MonthlyRent, unitType);
            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult<int>.Success(unit.Id);
        }, ServiceResult<int>.Failure);
    }

    public Task<ServiceResult> UpdateUnitAsync(int unitId, UnitDetails details, CancellationToken cancellationToken = default)
    {
        return RunAsync(async () =>
        {
            var property = await _db.Properties
                .Include(p => p.Units).ThenInclude(u => u.UnitType)
                .SingleOrDefaultAsync(p => p.Units.Any(u => u.Id == unitId), cancellationToken);

            var unit = property?.Units.SingleOrDefault(u => u.Id == unitId);
            if (property is null || unit is null)
                return ServiceResult.Failure(UnitNotFoundMessage);

            var unitType = details.UnitTypeId == unit.UnitTypeId
                ? unit.UnitType
                : await _db.UnitTypes.SingleOrDefaultAsync(t => t.Id == details.UnitTypeId, cancellationToken);
            if (unitType is null)
                return ServiceResult.Failure(UnitTypeNotFoundMessage);

            property.UpdateUnit(unit, details.UnitNumber, details.Bedrooms, details.MonthlyRent, unitType);
            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult.Success();
        }, ServiceResult.Failure);
    }

    public Task<ServiceResult> DeleteUnitAsync(int unitId, CancellationToken cancellationToken = default)
    {
        return RunAsync(async () =>
        {
            var unit = await _db.Units.SingleOrDefaultAsync(u => u.Id == unitId, cancellationToken);
            if (unit is null)
                return ServiceResult.Failure(UnitNotFoundMessage);

            if (await AnyUnitInUseAsync([unitId], cancellationToken))
                return ServiceResult.Failure(UnitInUseMessage);

            _db.Units.Remove(unit);
            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult.Success();
        }, ServiceResult.Failure);
    }

    private async Task<bool> AnyUnitInUseAsync(IReadOnlyCollection<int> unitIds, CancellationToken cancellationToken)
    {
        if (await _db.RentalApplications.AnyAsync(a => unitIds.Contains(a.UnitId), cancellationToken))
            return true;

        return await _db.Leases.AnyAsync(l => unitIds.Contains(l.UnitId), cancellationToken);
    }

    private async Task<TResult> RunAsync<TResult>(Func<Task<TResult>> operation, Func<string, TResult> failure)
        where TResult : ServiceResult
    {
        try
        {
            var result = await operation();
            if (!result.Succeeded)
                _db.ChangeTracker.Clear();

            return result;
        }
        catch (DomainException ex)
        {
            _db.ChangeTracker.Clear();
            return failure(ex.Message);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _db.ChangeTracker.Clear();
            return failure(DuplicateUnitNumberMessage);
        }
    }

    private static Address ToAddress(PropertyDetails details)
    {
        return new Address(details.Street, details.City, details.State, details.PostalCode);
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is SqlException { Number: SqlUniqueIndexViolation or SqlUniqueConstraintViolation };
    }
}
