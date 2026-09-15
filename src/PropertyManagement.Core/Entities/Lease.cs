using PropertyManagement.Core.Common;

namespace PropertyManagement.Core.Entities;

public class Lease : CreationAuditedEntity
{
    public const int TermMonths = 12;

    public const int MaxStartDaysAhead = 365;

    private Lease()
    {
    }

    internal Lease(int unitId, DateOnly startDate)
    {
        UnitId = unitId;
        StartDate = startDate;
        EndDate = EndDateFor(startDate);
    }

    public int Id { get; private set; }
    public int UnitId { get; private set; }
    public int RentalApplicationId { get; private set; }
    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public static DateOnly EndDateFor(DateOnly startDate) => startDate.AddMonths(TermMonths).AddDays(-1);

    public bool Covers(DateOnly date) => StartDate <= date && date <= EndDate;
}