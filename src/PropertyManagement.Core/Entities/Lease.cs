using PropertyManagement.Core.Common;

namespace PropertyManagement.Core.Entities;

public class Lease : CreationAuditedEntity
{
    public const int TermMonths = 12;
    public const int MaxStartDaysAhead = 365;
    public int Id { get; private set; }
    public int UnitId { get; private set; }
    public int RentalApplicationId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }

    private Lease()
    {
    }

    internal Lease(int unitId, DateOnly startDate)
    {
        UnitId = unitId;
        StartDate = startDate;
        EndDate = EndDateFor(startDate);
    }

    public static DateOnly EndDateFor(DateOnly startDate)
    {
        return startDate.AddMonths(TermMonths).AddDays(-1);
    }

    /// <summary>A lease may start today or later (in the business time zone), up to MaxStartDaysAhead days ahead.</summary>
    public static void EnsureValidStartDate(DateOnly startDate, DateOnly today)
    {
        if (startDate < today)
            throw new DomainException("The lease start date can't be in the past.");
        if (startDate > today.AddDays(MaxStartDaysAhead))
            throw new DomainException($"The lease must start within {MaxStartDaysAhead} days.");
    }

    public bool Covers(DateOnly date)
    {
        return StartDate <= date && date <= EndDate;
    }
}
