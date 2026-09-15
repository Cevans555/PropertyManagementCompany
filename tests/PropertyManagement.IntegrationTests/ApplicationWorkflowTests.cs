using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Dtos;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Enums;
using PropertyManagement.Core.ValueObjects;
using PropertyManagement.Data;
using PropertyManagement.Data.Services;
using PropertyManagement.IntegrationTests.Infrastructure;

namespace PropertyManagement.IntegrationTests;

public class ApplicationWorkflowTests : IClassFixture<TestDatabase>
{
    private readonly TestDatabase _database;

    public ApplicationWorkflowTests(TestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task Submit_PersistsSubmittedStatusAndHistory()
    {
        await using var db = _database.CreateContext();
        var applicantId = await _database.CreateUserAsync(db, "applicant");
        var applicationId = await ReadyToSubmitAsync(db, applicantId);

        var result = await _database.ApplicantService(db).SubmitAsync(applicationId, applicantId);

        Assert.True(result.Succeeded, result.Error);
        var saved = await ReloadAsync(applicationId);
        Assert.Equal(ApplicationStatus.Submitted, saved.Status);
        Assert.NotNull(saved.SubmittedAt);
        Assert.Contains(saved.StatusHistory, h => h.ToStatus == ApplicationStatus.Submitted);
    }

    [Fact]
    public async Task Claim_PersistsManagerAndUnderReview()
    {
        await using var db = _database.CreateContext();
        var applicantId = await _database.CreateUserAsync(db, "applicant");
        var managerId = await _database.CreateUserAsync(db, "manager");
        var applicationId = await SubmittedAsync(db, applicantId);

        var result = await _database.ReviewService(db).ClaimAsync(applicationId, managerId);

        Assert.True(result.Succeeded, result.Error);
        var saved = await ReloadAsync(applicationId);
        Assert.Equal(ApplicationStatus.UnderReview, saved.Status);
        Assert.Equal(managerId, saved.ClaimedById);
    }

    [Fact]
    public async Task Approve_CreatesLeaseForTheUnit()
    {
        await using var db = _database.CreateContext();
        var applicantId = await _database.CreateUserAsync(db, "applicant");
        var managerId = await _database.CreateUserAsync(db, "manager");
        var applicationId = await ClaimedAsync(db, applicantId, managerId);

        var result = await _database.ReviewService(db).ApproveAsync(applicationId, managerId, _database.Today, "Welcome");

        Assert.True(result.Succeeded, result.Error);
        await using var check = _database.CreateContext();
        var lease = await check.Leases.SingleAsync(l => l.RentalApplicationId == applicationId);
        Assert.Equal(_database.Today, lease.StartDate);
        Assert.Equal(Lease.EndDateFor(_database.Today), lease.EndDate);
    }

    [Fact]
    public async Task Approve_IsRejected_WhenTheUnitAlreadyHasALease()
    {
        await using var db = _database.CreateContext();
        var applicantId = await _database.CreateUserAsync(db, "applicant");
        var managerId = await _database.CreateUserAsync(db, "manager");
        var unitId = await _database.CreateUnitAsync(db);

        // Both applications reach review first: once a lease exists, a new application for the unit
        // can't even be started.
        var firstId = await ClaimedAsync(db, applicantId, managerId, unitId);
        var secondId = await ClaimedAsync(db, applicantId, managerId, unitId, submitWithLeaseCheck: false);

        var approved = await _database.ReviewService(db).ApproveAsync(firstId, managerId, _database.Today, null);
        Assert.True(approved.Succeeded, approved.Error);

        var result = await _database.ReviewService(db).ApproveAsync(secondId, managerId, _database.Today, null);

        Assert.False(result.Succeeded);
        await using var check = _database.CreateContext();
        Assert.Equal(1, await check.Leases.CountAsync(l => l.UnitId == unitId));
    }

    [Fact]
    public async Task StatusConcurrencyToken_RejectsAClaimMadeFromStaleData()
    {
        await using var setup = _database.CreateContext();
        var applicantId = await _database.CreateUserAsync(setup, "applicant");
        var firstManagerId = await _database.CreateUserAsync(setup, "manager");
        var secondManagerId = await _database.CreateUserAsync(setup, "manager");
        var applicationId = await SubmittedAsync(setup, applicantId);

        // Both managers load the same submitted application, then both claim it.
        await using var firstContext = _database.CreateContext();
        await using var secondContext = _database.CreateContext();
        var first = await firstContext.RentalApplications.SingleAsync(a => a.Id == applicationId);
        var second = await secondContext.RentalApplications.SingleAsync(a => a.Id == applicationId);

        first.Claim(firstManagerId, DateTime.UtcNow);
        await firstContext.SaveChangesAsync();

        second.Claim(secondManagerId, DateTime.UtcNow);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
        var saved = await ReloadAsync(applicationId);
        Assert.Equal(firstManagerId, saved.ClaimedById);
    }

    [Fact]
    public async Task TwoApprovalsForTheSameUnit_OnlyOneCreatesALease()
    {
        await using var setup = _database.CreateContext();
        var applicantId = await _database.CreateUserAsync(setup, "applicant");
        var managerId = await _database.CreateUserAsync(setup, "manager");
        var unitId = await _database.CreateUnitAsync(setup);
        var firstId = await ClaimedAsync(setup, applicantId, managerId, unitId);
        var secondId = await ClaimedAsync(setup, applicantId, managerId, unitId, submitWithLeaseCheck: false);

        // Two managers approving at the same moment, each on their own connection.
        await using var firstContext = _database.CreateContext();
        await using var secondContext = _database.CreateContext();
        var firstApproval = _database.ReviewService(firstContext).ApproveAsync(firstId, managerId, _database.Today, null);
        var secondApproval = _database.ReviewService(secondContext).ApproveAsync(secondId, managerId, _database.Today, null);
        var results = await Task.WhenAll(firstApproval, secondApproval);

        Assert.Equal(1, results.Count(r => r.Succeeded));
        await using var check = _database.CreateContext();
        Assert.Equal(1, await check.Leases.CountAsync(l => l.UnitId == unitId));
    }

    // ---- Helpers -----------------------------------------------------------------------------

    private async Task<int> ReadyToSubmitAsync(PropertyManagementDbContext db, string applicantId, int? unitId = null)
    {
        var id = unitId ?? await _database.CreateUnitAsync(db);
        var started = await _database.ApplicantService(db).StartAsync(id, applicantId);
        Assert.True(started.Succeeded, started.Error);

        // The section-saving service methods arrive with the application page, so fill the sections
        // through the domain methods for now.
        var application = await db.RentalApplications
            .Include(a => a.Applicants)
            .Include(a => a.Residences)
            .SingleAsync(a => a.Id == started.Value);

        application.SaveApplicantDetails(applicantId, applicantId, Details(), DateTime.UtcNow);
        application.AddResidence(applicantId, Residence());
        application.SaveResidenceSection(applicantId, DateTime.UtcNow);
        await db.SaveChangesAsync();

        return application.Id;
    }

    private async Task<int> SubmittedAsync(PropertyManagementDbContext db, string applicantId, int? unitId = null)
    {
        var applicationId = await ReadyToSubmitAsync(db, applicantId, unitId);
        var result = await _database.ApplicantService(db).SubmitAsync(applicationId, applicantId);
        Assert.True(result.Succeeded, result.Error);
        return applicationId;
    }

    /// <param name="submitWithLeaseCheck">
    /// False for a second application on a unit that already has a lease: Submit would refuse, so the
    /// domain methods are used directly to reach Under Review.
    /// </param>
    private async Task<int> ClaimedAsync(
        PropertyManagementDbContext db, string applicantId, string managerId, int? unitId = null, bool submitWithLeaseCheck = true)
    {
        if (submitWithLeaseCheck)
        {
            var applicationId = await SubmittedAsync(db, applicantId, unitId);
            var claimed = await _database.ReviewService(db).ClaimAsync(applicationId, managerId);
            Assert.True(claimed.Succeeded, claimed.Error);
            return applicationId;
        }

        var readyId = await ReadyToSubmitAsync(db, applicantId, unitId);
        var application = await db.RentalApplications
            .Include(a => a.Applicants)
            .Include(a => a.Residences)
            .SingleAsync(a => a.Id == readyId);

        application.Submit(applicantId, unitHasActiveLease: false, DateTime.UtcNow);
        application.Claim(managerId, DateTime.UtcNow);
        await db.SaveChangesAsync();
        return readyId;
    }

    private async Task<RentalApplication> ReloadAsync(int applicationId)
    {
        await using var db = _database.CreateContext();
        return await db.RentalApplications
            .Include(a => a.Applicants)
            .Include(a => a.StatusHistory)
            .AsSplitQuery()
            .SingleAsync(a => a.Id == applicationId);
    }

    private static ApplicantDetails Details()
    {
        return new ApplicantDetails
        {
            FirstName = "Jane",
            LastName = "Doe",
            Phone = "(518) 555-0100",
            Email = "jane@example.com",
            Street = "1 Main St",
            City = "Albany",
            State = "NY",
            PostalCode = "12207"
        };
    }

    private static ResidenceDetails Residence()
    {
        return new ResidenceDetails
        {
            Address = new Address("22 Elm St", "Troy", "NY", "12180"),
            LandlordName = "Bob Landlord",
            LandlordPhone = "(518) 555-0199",
            MoveInDate = new DateOnly(2022, 1, 1),
            MoveOutDate = new DateOnly(2025, 12, 31)
        };
    }
}
