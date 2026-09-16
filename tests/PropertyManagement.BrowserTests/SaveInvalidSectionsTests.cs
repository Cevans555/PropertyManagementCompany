using Microsoft.Playwright;
using PropertyManagement.BrowserTests.Infrastructure;
using PropertyManagement.Core.Entities;
using PropertyManagement.IntegrationTests.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace PropertyManagement.BrowserTests;

[Collection(BrowserCollection.Name)]
public class SaveInvalidSectionsTests
{
    private readonly BrowserFixture _fixture;

    public SaveInvalidSectionsTests(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveWithErrors_SummaryListsThem_FixFromSummary_ThenSubmit()
    {
        using var _ = _fixture.App.EnableSaveInvalidSections();
        var applicationId = await DraftWithOneResidenceAsync();
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Applicant);
        await using var __ = context;
        var continueButton = page.GetByRole(AriaRole.Button, new() { Name = "Continue" });

        await page.GotoAsync($"/Applications/Details/{applicationId}");
        await continueButton.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Residence history" })).ToBeVisibleAsync();
        await Expect(page.GetByText("Your applicant information was saved with errors.")).ToBeVisibleAsync();

        await continueButton.ClickAsync();
        await Expect(page.GetByText("Phone is required.")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Submit application" })).ToBeDisabledAsync();

        await page.GetByRole(AriaRole.Link, new() { Name = "Fix" }).First.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Applicant information" })).ToBeVisibleAsync();

        await page.GetByLabel("Phone").FillAsync("(518) 555-0100");
        await page.GetByLabel("Street", new() { Exact = true }).FillAsync("1 Main St");
        await page.GetByLabel("City", new() { Exact = true }).FillAsync("Albany");
        await page.GetByLabel("State", new() { Exact = true }).FillAsync("NY");
        await page.GetByLabel("Postal code").FillAsync("12207");
        await continueButton.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Residence history" })).ToBeVisibleAsync();
        await continueButton.ClickAsync();

        await Expect(page.GetByText("Everything is saved.")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Submit application" }).ClickAsync();
        await Expect(page.GetByText("Your application was submitted.")).ToBeVisibleAsync();
    }

    private async Task<int> DraftWithOneResidenceAsync()
    {
        var unitId = await _fixture.App.CreateUnitAsync();
        var applicantId = await _fixture.App.UserIdAsync(TestAccounts.Applicant);

        return await _fixture.App.QueryDbAsync(async db =>
        {
            var application = RentalApplication.Start(unitId, applicantId, unitHasActiveLease: false, DateTime.UtcNow);
            application.AddResidence(applicantId, TestHelpers.Residence());
            db.RentalApplications.Add(application);
            await db.SaveChangesAsync();
            return application.Id;
        });
    }
}
