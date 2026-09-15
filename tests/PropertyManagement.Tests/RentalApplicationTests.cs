using PropertyManagement.Core.Common;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Enums;
using static PropertyManagement.Tests.TestSupport.TestData;

namespace PropertyManagement.Tests;

public class RentalApplicationTests
{
    [Fact]
    public void Start_CreatesDraftWithPrimaryApplicantAndHistory()
    {
        var application = RentalApplication.Start(1, ApplicantId, unitHasActiveLease: false, Now);

        Assert.Equal(ApplicationStatus.Draft, application.Status);
        var applicant = Assert.Single(application.Applicants);
        Assert.True(applicant.IsPrimary);
        var history = Assert.Single(application.StatusHistory);
        Assert.Null(history.FromStatus);
        Assert.Equal(ApplicationStatus.Draft, history.ToStatus);
    }

    [Fact]
    public void Start_Throws_WhenUnitHasActiveLease()
    {
        Assert.Throws<DomainException>(() => RentalApplication.Start(1, ApplicantId, unitHasActiveLease: true, Now));
    }

    [Fact]
    public void Submit_Throws_WhenSectionsAreNotSaved()
    {
        var application = Draft();

        Assert.Throws<DomainException>(() => application.Submit(ApplicantId, unitHasActiveLease: false, Now));
        Assert.Equal(ApplicationStatus.Draft, application.Status);
    }

    [Fact]
    public void Submit_Throws_WhenCoApplicantDetailsAreMissing()
    {
        var application = ReadyToSubmit();
        application.AddApplicant(ApplicantId, OtherApplicantId);

        Assert.False(application.CanSubmit);
        Assert.Throws<DomainException>(() => application.Submit(ApplicantId, unitHasActiveLease: false, Now));
    }

    [Fact]
    public void Submit_Throws_WhenUnitHasActiveLease()
    {
        var application = ReadyToSubmit();

        Assert.Throws<DomainException>(() => application.Submit(ApplicantId, unitHasActiveLease: true, Now));
    }

    [Fact]
    public void Submit_MovesToSubmitted_WhenComplete()
    {
        var application = ReadyToSubmit();

        application.Submit(ApplicantId, unitHasActiveLease: false, Now);

        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Equal(Now, application.SubmittedAt);
        Assert.False(application.IsEditable);
    }

    [Fact]
    public void Edits_Throw_OnceSubmitted()
    {
        var application = Submitted();

        Assert.Throws<DomainException>(() => application.AddResidence(ApplicantId, Residence()));
        Assert.Throws<DomainException>(() => application.SaveApplicantDetails(ApplicantId, ApplicantId, Details(), Now));
    }

    [Fact]
    public void Edits_Throw_ForUserNotOnApplication()
    {
        var application = ReadyToSubmit();

        Assert.Throws<DomainException>(() => application.AddResidence("stranger", Residence()));
    }

    [Fact]
    public void Review_RequiresClaimFirst()
    {
        var application = Submitted();

        Assert.Throws<DomainException>(() => application.Approve(ManagerId, Today, Today, false, null, Now));
    }

    [Fact]
    public void Review_Throws_WhenClaimedByAnotherManager()
    {
        var application = Claimed();

        Assert.Throws<DomainException>(() => application.Deny(OtherManagerId, "No", Now));
    }

    [Fact]
    public void Release_ReturnsApplicationToQueue()
    {
        var application = Claimed();

        application.Release(ManagerId, Now);

        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Null(application.ClaimedById);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void ReturnAndDeny_RequireComment(string? comment)
    {
        var application = Claimed();

        Assert.Throws<DomainException>(() => application.Return(ManagerId, comment!, Now));
        Assert.Throws<DomainException>(() => application.Deny(ManagerId, comment!, Now));
        Assert.Equal(ApplicationStatus.UnderReview, application.Status);
    }

    [Fact]
    public void Return_AllowsApplicantToEditAndResubmit()
    {
        var application = Claimed();

        application.Return(ManagerId, "Fix your phone number", Now);
        application.SaveApplicantDetails(ApplicantId, ApplicantId, Details(), Now);
        application.Submit(ApplicantId, unitHasActiveLease: false, Now);

        Assert.Equal(ApplicationStatus.Submitted, application.Status);
    }

    [Fact]
    public void Approve_CreatesTwelveMonthLease()
    {
        var application = Claimed();

        var lease = application.Approve(ManagerId, Today, Today, unitHasConflictingLease: false, comment: null, Now);

        Assert.Equal(ApplicationStatus.Approved, application.Status);
        Assert.Equal(new DateOnly(2026, 9, 1), lease.StartDate);
        Assert.Equal(new DateOnly(2027, 8, 31), lease.EndDate);
        Assert.True(lease.Covers(new DateOnly(2027, 8, 31)));
        Assert.False(lease.Covers(new DateOnly(2027, 9, 1)));
    }

    [Fact]
    public void Approve_Throws_WhenUnitHasActiveLease()
    {
        var application = Claimed();

        Assert.Throws<DomainException>(() => application.Approve(ManagerId, Today, Today, unitHasConflictingLease: true, null, Now));
        Assert.Null(application.Lease);
    }

    [Theory]
    [InlineData(ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Denied)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public void TerminalStatuses_CannotBeWithdrawn(ApplicationStatus terminal)
    {
        var application = Submitted();

        if (terminal == ApplicationStatus.Withdrawn)
        {
            application.Withdraw(ApplicantId, Now);
        }
        else if (terminal == ApplicationStatus.Approved)
        {
            application.Claim(ManagerId, Now);
            application.Approve(ManagerId, Today, Today, false, null, Now);
        }
        else
        {
            application.Claim(ManagerId, Now);
            application.Deny(ManagerId, "No", Now);
        }

        Assert.Throws<DomainException>(() => application.Withdraw(ApplicantId, Now));
    }

    [Fact]
    public void StatusHistory_RecordsEveryChange()
    {
        var application = Claimed();
        application.Deny(ManagerId, "Rental history could not be verified", Now);

        Assert.Equal(
            [ApplicationStatus.Draft, ApplicationStatus.Submitted, ApplicationStatus.UnderReview, ApplicationStatus.Denied],
            application.StatusHistory.Select(h => h.ToStatus));
        Assert.Equal("Rental history could not be verified", application.StatusHistory.Last().Comment);
    }

    [Fact]
    public void ResidenceEdits_ChangeSectionVersion()
    {
        var application = Draft();
        var before = application.ResidenceSectionVersion;

        application.AddResidence(ApplicantId, Residence());

        Assert.NotEqual(before, application.ResidenceSectionVersion);
    }
}
