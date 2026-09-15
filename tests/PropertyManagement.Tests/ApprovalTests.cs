using PropertyManagement.Core.Common;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Enums;
using static PropertyManagement.Tests.TestSupport.TestData;

namespace PropertyManagement.Tests;

public class ApprovalTests
{
    [Fact]
    public void Approve_AllowsTodayAsStartDate()
    {
        var application = Claimed();

        var lease = application.Approve(ManagerId, Today, Today, unitHasConflictingLease: false, comment: null, Now);

        Assert.Equal(Today, lease.StartDate);
        Assert.Equal(ApplicationStatus.Approved, application.Status);
    }

    [Fact]
    public void Approve_UsesChosenFutureStartDate()
    {
        var application = Claimed();
        var startDate = new DateOnly(2026, 10, 1);

        var lease = application.Approve(ManagerId, startDate, Today, false, null, Now);

        Assert.Equal(startDate, lease.StartDate);
        Assert.Equal(new DateOnly(2027, 9, 30), lease.EndDate);
    }

    [Fact]
    public void Approve_Throws_WhenStartDateIsInThePast()
    {
        var application = Claimed();

        Assert.Throws<DomainException>(() => application.Approve(ManagerId, Today.AddDays(-1), Today, false, null, Now));
        Assert.Equal(ApplicationStatus.UnderReview, application.Status);
    }

    [Fact]
    public void Approve_Throws_WhenStartDateIsTooFarAhead()
    {
        var application = Claimed();

        Assert.Throws<DomainException>(() =>
            application.Approve(ManagerId, Today.AddDays(Lease.MaxStartDaysAhead + 1), Today, false, null, Now));
    }

    [Fact]
    public void Approve_Throws_WhenUnitHasConflictingLease()
    {
        var application = Claimed();

        Assert.Throws<DomainException>(() =>
            application.Approve(ManagerId, Today.AddDays(30), Today, unitHasConflictingLease: true, null, Now));
        Assert.Null(application.Lease);
    }

    [Fact]
    public void Withdraw_IsAllowedWhileUnderReview()
    {
        var application = Claimed();

        application.Withdraw(ApplicantId, Now);

        Assert.Equal(ApplicationStatus.Withdrawn, application.Status);
        Assert.Null(application.ClaimedById);
    }

    [Fact]
    public void EndDateFor_IsTwelveMonthsInclusive()
    {
        Assert.Equal(new DateOnly(2027, 2, 27), Lease.EndDateFor(new DateOnly(2026, 2, 28)));
    }
}
