using Microsoft.Playwright;
using PropertyManagement.BrowserTests.Infrastructure;
using PropertyManagement.IntegrationTests.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace PropertyManagement.BrowserTests;

[Collection(BrowserCollection.Name)]
public class ReviewWorkflowTests
{
    private readonly BrowserFixture _fixture;

    public ReviewWorkflowTests(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ClaimFromQueue_ThenReleaseBackToQueue()
    {
        var applicationId = await SubmittedApplicationAsync();
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;

        await page.GotoAsync("/Reviews/Queue");
        await page.Locator($"form[action$='/Reviews/Claim/{applicationId}'] button").ClickAsync();
        await Expect(page.GetByText("You claimed this application for review.")).ToBeVisibleAsync();
        await Expect(page.Locator(".badge", new() { HasText = "Under Review" })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Release to queue" }).ClickAsync();
        await Expect(page.GetByText("The application is back in the review queue.")).ToBeVisibleAsync();

        await page.GotoAsync("/Reviews/Queue");
        await Expect(page.Locator("section[aria-labelledby='waiting-heading']").Locator($"form[action$='/Reviews/Claim/{applicationId}']")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task ReturnWithComment_ApplicantSeesItAndCanEditAgain()
    {
        var applicationId = await SubmittedApplicationAsync();
        var (managerContext, manager) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = managerContext;
        await manager.GotoAsync($"/Applications/Details/{applicationId}");
        var modal = manager.Modal();

        await manager.GetByRole(AriaRole.Button, new() { Name = "Claim for review" }).ClickAsync();
        await manager.GetByRole(AriaRole.Button, new() { Name = "Complete review" }).ClickAsync();
        await modal.GetByLabel("Return").CheckAsync();
        await modal.GetByLabel("Comment").FillAsync("Please add your current landlord.");
        await modal.GetByRole(AriaRole.Button, new() { Name = "Submit review" }).ClickAsync();
        await Expect(manager.Locator(".badge", new() { HasText = "Returned" })).ToBeVisibleAsync();

        var (applicantContext, applicant) = await _fixture.SignInAsync(TestAccounts.Applicant);
        await using var __ = applicantContext;
        await applicant.GotoAsync($"/Applications/Details/{applicationId}");

        await Expect(applicant.Locator(".badge", new() { HasText = "Returned" })).ToBeVisibleAsync();
        await Expect(applicant.GetByRole(AriaRole.Button, new() { Name = "Continue" })).ToBeVisibleAsync();
        await Expect(applicant.GetByLabel("Phone")).ToBeEditableAsync();
    }

    [Fact]
    public async Task ManagerNotes_AddEditRemove_WithoutReload_AndHiddenFromApplicant()
    {
        var applicationId = await SubmittedApplicationAsync();
        var noteText = $"Browser note {Guid.NewGuid():N}";
        var editedText = noteText + " (edited)";
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;
        await page.GotoAsync($"/Applications/Details/{applicationId}");
        await page.MarkPageAsync();
        var modal = page.Modal();
        var notes = page.Locator("#manager-notes");

        await page.GetByRole(AriaRole.Button, new() { Name = "Add note" }).ClickAsync();
        await modal.GetByLabel("Note").FillAsync(noteText);
        await modal.GetByRole(AriaRole.Button, new() { Name = "Add note" }).ClickAsync();
        await Expect(modal).ToBeHiddenAsync();
        await Expect(notes).ToContainTextAsync(noteText);

        await notes.GetByRole(AriaRole.Button, new() { Name = "Edit" }).First.ClickAsync();
        await modal.GetByLabel("Note").FillAsync(editedText);
        await modal.GetByRole(AriaRole.Button, new() { Name = "Save changes" }).ClickAsync();
        await Expect(modal).ToBeHiddenAsync();
        await Expect(notes).ToContainTextAsync(editedText);

        await page.AssertNotReloadedAsync();

        var (applicantContext, applicant) = await _fixture.SignInAsync(TestAccounts.Applicant);
        await using var __ = applicantContext;
        await applicant.GotoAsync($"/Applications/Details/{applicationId}");
        await Expect(applicant.GetByText(editedText)).ToHaveCountAsync(0);
        await Expect(applicant.Locator("#manager-notes")).ToHaveCountAsync(0);

        await notes.GetByRole(AriaRole.Button, new() { Name = "Remove" }).First.ClickAsync();
        await modal.GetByRole(AriaRole.Button, new() { Name = "Remove", Exact = true }).ClickAsync();
        await Expect(modal).ToBeHiddenAsync();
        await Expect(notes).Not.ToContainTextAsync(editedText);
    }

    [Fact]
    public async Task ManagerSeesStatusHistory_ApplicantDoesNot()
    {
        var applicationId = await SubmittedApplicationAsync();

        var (managerContext, manager) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = managerContext;
        await manager.GotoAsync($"/Applications/Details/{applicationId}");
        await Expect(manager.GetByRole(AriaRole.Heading, new() { Name = "Status history" })).ToBeVisibleAsync();

        var (applicantContext, applicant) = await _fixture.SignInAsync(TestAccounts.Applicant);
        await using var __ = applicantContext;
        await applicant.GotoAsync($"/Applications/Details/{applicationId}");
        await Expect(applicant.GetByRole(AriaRole.Heading, new() { Name = "Status history" })).ToHaveCountAsync(0);
    }

    private async Task<int> SubmittedApplicationAsync()
    {
        var unitId = await _fixture.App.CreateUnitAsync();
        var applicantId = await _fixture.App.UserIdAsync(TestAccounts.Applicant);
        return await _fixture.App.CreateApplicationAsync(unitId, applicantId);
    }
}
