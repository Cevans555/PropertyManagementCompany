using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using PropertyManagement.BrowserTests.Infrastructure;
using PropertyManagement.IntegrationTests.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace PropertyManagement.BrowserTests;

[Collection(BrowserCollection.Name)]
public class ExistingLeaseWarningTests
{
    private readonly BrowserFixture _fixture;

    public ExistingLeaseWarningTests(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ReviewModal_ShowsLeaseTheApplicantAlreadyHolds_WithoutBlockingApproval()
    {
        var app = _fixture.App;
        var managerId = await app.UserIdAsync(TestAccounts.Manager);
        var applicantId = await app.UserIdAsync(TestAccounts.OtherApplicant);
        var leasedUnit = await app.CreateUnitAsync();
        var leased = await app.CreateApplicationAsync(leasedUnit, applicantId, claimedBy: managerId);
        var today = app.BusinessToday();
        await app.QueryDbAsync(async db =>
        {
            var application = await db.RentalApplications.SingleAsync(a => a.Id == leased);
            application.Approve(managerId, today, today, unitHasConflictingLease: false, comment: null, DateTime.UtcNow);
            return await db.SaveChangesAsync();
        });
        var propertyName = await app.QueryDbAsync(db =>
            db.Units.Where(u => u.Id == leasedUnit).Select(u => u.Property.Name).SingleAsync());
        var claimed = await app.CreateApplicationAsync(await app.CreateUnitAsync(), applicantId, claimedBy: managerId);

        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;
        await page.GotoAsync($"/Applications/Details/{claimed}");
        await page.GetByRole(AriaRole.Button, new() { Name = "Complete review" }).ClickAsync();

        var modal = page.Modal();
        await Expect(modal).ToContainTextAsync("Already holds a lease.");
        await Expect(modal.Locator($"[data-lease-application='{leased}']")).ToContainTextAsync(propertyName);
        await Expect(modal.GetByLabel("Approve")).ToBeEnabledAsync();
    }
}
