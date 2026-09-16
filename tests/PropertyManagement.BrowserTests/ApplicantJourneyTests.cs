using Microsoft.Playwright;
using PropertyManagement.BrowserTests.Infrastructure;
using PropertyManagement.IntegrationTests.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace PropertyManagement.BrowserTests;

[Collection(BrowserCollection.Name)]
public class ApplicantJourneyTests
{
    private readonly BrowserFixture _fixture;

    public ApplicantJourneyTests(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ApplyFillSectionsAddResidenceAndSubmit()
    {
        var unitId = await _fixture.App.CreateUnitAsync();
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Applicant);
        await using var _ = context;

        await page.GotoAsync("/Applications/Available");
        await page.Locator($"form:has(input[name='unitId'][value='{unitId}']) button").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Applicant information" })).ToBeVisibleAsync();

        await page.GetByLabel("Phone").FillAsync("(518) 555-0100");
        await page.GetByLabel("Street", new() { Exact = true }).FillAsync("1 Main St");
        await page.GetByLabel("City", new() { Exact = true }).FillAsync("Albany");
        await page.GetByLabel("State", new() { Exact = true }).FillAsync("NY");
        await page.GetByLabel("Postal code").FillAsync("12207");
        await page.GetByRole(AriaRole.Button, new() { Name = "Continue" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Residence history" })).ToBeVisibleAsync();
        await page.MarkPageAsync();
        var modal = page.Modal();
        await page.Locator("[data-modal-url*='/ApplicationResidences/Create']").ClickAsync();
        await modal.GetByLabel("Street", new() { Exact = true }).FillAsync("22 Elm St");
        await modal.GetByLabel("City", new() { Exact = true }).FillAsync("Troy");
        await modal.GetByLabel("State", new() { Exact = true }).FillAsync("NY");
        await modal.GetByLabel("Postal code").FillAsync("12180");
        await modal.GetByLabel("Landlord name").FillAsync("Bob Landlord");
        await modal.GetByLabel("Landlord phone").FillAsync("(518) 555-0199");
        await modal.GetByLabel("Move-in date").FillAsync("2022-01-01");
        await modal.GetByLabel("Move-out date").FillAsync("2025-12-31");
        await modal.GetByRole(AriaRole.Button, new() { Name = "Add residence" }).ClickAsync();

        await Expect(modal).ToBeHiddenAsync();
        await Expect(page.Locator("#residence-list")).ToContainTextAsync("Bob Landlord");
        await page.AssertNotReloadedAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Continue" }).ClickAsync();
        await Expect(page.GetByText("Everything is saved.")).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Submit application" }).ClickAsync();
        await Expect(page.GetByText("Your application was submitted.")).ToBeVisibleAsync();
        await Expect(page.Locator(".badge", new() { HasText = "Submitted" })).ToBeVisibleAsync();
    }
}
