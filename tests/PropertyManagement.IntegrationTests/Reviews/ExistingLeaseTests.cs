using Microsoft.EntityFrameworkCore;
using PropertyManagement.IntegrationTests.Infrastructure;

namespace PropertyManagement.IntegrationTests;

[Collection(WebCollection.Name)]
public class ExistingLeaseTests
{
    private readonly PropertyManagementWebFactory _factory;

    public ExistingLeaseTests(PropertyManagementWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Manager_SeesLeaseHeldByApplicant_OnPage_InReviewModal_AndInQueue()
    {
        var leased = await LeasedApplicationAsync(TestAccounts.OtherApplicant);
        var claimed = await ApplicationAsync(TestAccounts.OtherApplicant, claimed: true);
        var waiting = await ApplicationAsync(TestAccounts.OtherApplicant, claimed: false);
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        Assert.Contains(LeaseMarker(leased), await client.GetStringAsync($"/Applications/Details/{claimed}"));
        Assert.Contains(LeaseMarker(leased), await client.GetStringAsync($"/Reviews/Review/{claimed}"));

        var queue = await client.GetStringAsync("/Reviews/Queue");
        Assert.Contains($"data-has-lease=\"{waiting}\"", queue);
        Assert.Contains($"data-has-lease=\"{claimed}\"", queue);
    }

    [Fact]
    public async Task Applicant_NeverSeesTheWarning()
    {
        await LeasedApplicationAsync(TestAccounts.OtherApplicant);
        var claimed = await ApplicationAsync(TestAccounts.OtherApplicant, claimed: true);
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.OtherApplicant);

        var html = await client.GetStringAsync($"/Applications/Details/{claimed}");

        Assert.DoesNotContain("data-existing-lease-notice", html);
    }

    [Fact]
    public async Task LeaseHeldByCoApplicant_IsShown()
    {
        var leased = await LeasedApplicationAsync(TestAccounts.Applicant);
        var draft = await ApplicationAsync(TestAccounts.OtherApplicant, claimed: false, submit: false);
        var primaryId = await _factory.UserIdAsync(TestAccounts.OtherApplicant);
        var coApplicantId = await _factory.UserIdAsync(TestAccounts.Applicant);
        await _factory.QueryDbAsync(async db =>
        {
            var application = await db.RentalApplications.Include(a => a.Applicants).SingleAsync(a => a.Id == draft);
            application.AddApplicant(primaryId, coApplicantId);
            return await db.SaveChangesAsync();
        });
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        Assert.Contains(LeaseMarker(leased), await client.GetStringAsync($"/Applications/Details/{draft}"));
    }

    [Fact]
    public async Task EndedLease_IsIgnored()
    {
        var leased = await LeasedApplicationAsync(TestAccounts.OtherApplicant);
        await _factory.QueryDbAsync(db => db.Leases
            .Where(l => l.RentalApplicationId == leased)
            .ExecuteUpdateAsync(set => set
                .SetProperty(l => l.StartDate, new DateOnly(2020, 1, 1))
                .SetProperty(l => l.EndDate, new DateOnly(2020, 12, 31))));
        var claimed = await ApplicationAsync(TestAccounts.OtherApplicant, claimed: true);
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        Assert.DoesNotContain(LeaseMarker(leased), await client.GetStringAsync($"/Applications/Details/{claimed}"));
    }

    [Fact]
    public async Task ApplicationsOwnLease_IsNotAWarning()
    {
        var leased = await LeasedApplicationAsync(TestAccounts.OtherApplicant);
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        Assert.DoesNotContain(LeaseMarker(leased), await client.GetStringAsync($"/Applications/Details/{leased}"));
    }

    private static string LeaseMarker(int leaseApplicationId)
    {
        return $"data-lease-application=\"{leaseApplicationId}\"";
    }

    private async Task<int> ApplicationAsync(string applicantEmail, bool claimed, bool submit = true)
    {
        return await _factory.CreateApplicationAsync(
            await _factory.CreateUnitAsync(),
            await _factory.UserIdAsync(applicantEmail),
            submit,
            claimedBy: claimed ? await _factory.UserIdAsync(TestAccounts.Manager) : null);
    }

    private async Task<int> LeasedApplicationAsync(string applicantEmail)
    {
        var managerId = await _factory.UserIdAsync(TestAccounts.Manager);
        var id = await ApplicationAsync(applicantEmail, claimed: true);
        var today = _factory.BusinessToday();

        await _factory.QueryDbAsync(async db =>
        {
            var application = await db.RentalApplications.SingleAsync(a => a.Id == id);
            application.Approve(managerId, today, today, unitHasConflictingLease: false, comment: null, DateTime.UtcNow);
            return await db.SaveChangesAsync();
        });

        return id;
    }
}
