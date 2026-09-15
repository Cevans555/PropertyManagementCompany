using PropertyManagement.Core.Common;
using PropertyManagement.Tests.TestSupport;
using static PropertyManagement.Tests.TestSupport.TestData;

namespace PropertyManagement.Tests;

public class BusinessTimeTests
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private static readonly DateTimeOffset EveningInEastern = new(2026, 9, 15, 1, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Today_IsTheDateInTheBusinessTimeZone_NotTheUtcDate()
    {
        var clock = new FixedClock(EveningInEastern, Eastern);

        Assert.Equal(new DateOnly(2026, 9, 14), clock.Today());
        Assert.Equal(new DateOnly(2026, 9, 15), DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime));
    }

    [Fact]
    public void BusinessTimeProvider_UsesTheConfiguredZone()
    {
        Assert.Equal(Eastern, new BusinessTimeProvider(Eastern).LocalTimeZone);
    }

    [Fact]
    public void Approve_AcceptsLeaseStartingOnTheApprovalDay_InTheEvening()
    {
        var clock = new FixedClock(EveningInEastern, Eastern);
        var now = clock.GetUtcNow().UtcDateTime;
        var application = Claimed(now);

        var lease = application.Approve(ManagerId, clock.Today(), clock.Today(), unitHasConflictingLease: false, comment: null, now);

        Assert.Equal(new DateOnly(2026, 9, 14), lease.StartDate);
    }

    [Fact]
    public void Approve_StillRejectsYesterday()
    {
        var clock = new FixedClock(EveningInEastern, Eastern);
        var now = clock.GetUtcNow().UtcDateTime;
        var application = Claimed(now);

        Assert.Throws<DomainException>(() =>
            application.Approve(ManagerId, clock.Today().AddDays(-1), clock.Today(), unitHasConflictingLease: false, comment: null, now));
    }
}
