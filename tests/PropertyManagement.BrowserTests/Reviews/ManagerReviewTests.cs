using System.Globalization;
using Microsoft.Playwright;
using PropertyManagement.BrowserTests.Infrastructure;
using PropertyManagement.IntegrationTests.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace PropertyManagement.BrowserTests;

[Collection(BrowserCollection.Name)]
public class ManagerReviewTests
{
    private const string CommentRequired = "A comment is required to return or deny an application.";

    private readonly BrowserFixture _fixture;

    public ManagerReviewTests(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ReviewModal_LeaseDateOnlyForApprove_DenyNeedsComment()
    {
        var applicationId = await SubmittedApplicationAsync();
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;
        await page.GotoAsync($"/Applications/Details/{applicationId}");
        var modal = page.Modal();

        await page.GetByRole(AriaRole.Button, new() { Name = "Claim for review" }).ClickAsync();
        await Expect(page.GetByText("You're reviewing this application.")).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Complete review" }).ClickAsync();
        var leaseStartDate = modal.GetByLabel("Lease start date");
        var deny = modal.GetByLabel("Deny");
        await Expect(deny).ToBeVisibleAsync();
        await Expect(leaseStartDate).ToBeHiddenAsync();

        await modal.GetByLabel("Approve").CheckAsync();
        await Expect(leaseStartDate).ToBeVisibleAsync();

        await deny.CheckAsync();
        await Expect(leaseStartDate).ToBeHiddenAsync();

        await modal.GetByRole(AriaRole.Button, new() { Name = "Submit review" }).ClickAsync();
        await Expect(modal).ToContainTextAsync(CommentRequired);
        await Expect(deny).ToBeCheckedAsync();
        await Expect(leaseStartDate).ToBeHiddenAsync();

        await modal.GetByLabel("Comment").FillAsync("Income could not be verified.");
        await modal.GetByRole(AriaRole.Button, new() { Name = "Submit review" }).ClickAsync();

        await Expect(page.Locator(".badge", new() { HasText = "Denied" })).ToBeVisibleAsync();
        await Expect(page.GetByText("Income could not be verified.")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ClaimThenApproveWithChosenStartDate()
    {
        var applicationId = await SubmittedApplicationAsync();
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;
        await page.GotoAsync($"/Applications/Details/{applicationId}");
        var modal = page.Modal();

        await page.GetByRole(AriaRole.Button, new() { Name = "Claim for review" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Complete review" }).ClickAsync();
        await modal.GetByLabel("Approve").CheckAsync();
        var startDate = _fixture.App.BusinessToday().AddDays(7);
        await modal.GetByLabel("Lease start date").FillAsync(startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        await modal.GetByRole(AriaRole.Button, new() { Name = "Submit review" }).ClickAsync();

        await Expect(page.Locator(".badge", new() { HasText = "Approved" })).ToBeVisibleAsync();
    }

    private async Task<int> SubmittedApplicationAsync()
    {
        var unitId = await _fixture.App.CreateUnitAsync();
        var applicantId = await _fixture.App.UserIdAsync(TestAccounts.Applicant);
        return await _fixture.App.CreateApplicationAsync(unitId, applicantId);
    }
}
