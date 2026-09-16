using Microsoft.Playwright;
using PropertyManagement.BrowserTests.Infrastructure;
using PropertyManagement.IntegrationTests.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace PropertyManagement.BrowserTests;

[Collection(BrowserCollection.Name)]
public class PropertyModalTests
{
    private readonly BrowserFixture _fixture;

    public PropertyModalTests(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddProperty_ValidatesInModal_ThenRefreshesListWithoutReload()
    {
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;
        await page.GotoAsync("/Properties");
        await page.MarkPageAsync();
        var modal = page.Modal();

        await page.Locator("[data-modal-url$='/Properties/Create']").ClickAsync();
        await Expect(modal).ToBeVisibleAsync();

        await modal.GetByRole(AriaRole.Button, new() { Name = "Add property" }).ClickAsync();
        await Expect(modal.GetByText("The Name field is required.")).ToBeVisibleAsync();

        var name = TestHelpers.UniqueName("Browser property");
        await modal.GetByLabel("Name", new() { Exact = true }).FillAsync(name);
        await modal.GetByLabel("Street", new() { Exact = true }).FillAsync("5 River Rd");
        await modal.GetByLabel("City", new() { Exact = true }).FillAsync("Troy");
        await modal.GetByLabel("State", new() { Exact = true }).FillAsync("NY");
        await modal.GetByLabel("Postal code").FillAsync("12180");
        await modal.GetByRole(AriaRole.Button, new() { Name = "Add property" }).ClickAsync();

        await Expect(modal).ToBeHiddenAsync();
        await Expect(page.Locator("#property-list")).ToContainTextAsync(name);
        await page.AssertNotReloadedAsync();
    }
}
