using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using PropertyManagement.BrowserTests.Infrastructure;
using PropertyManagement.IntegrationTests.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace PropertyManagement.BrowserTests;

[Collection(BrowserCollection.Name)]
public class PropertyEditTests
{
    private readonly BrowserFixture _fixture;

    public PropertyEditTests(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EditProperty_RenamesIt_InTheListWithoutReload()
    {
        var propertyId = await NewPropertyIdAsync();
        var newName = TestHelpers.UniqueName("Renamed property");
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;
        await page.GotoAsync("/Properties");
        await page.MarkPageAsync();
        var modal = page.Modal();

        await page.Locator($"[data-modal-url$='/Properties/Edit/{propertyId}']").ClickAsync();
        await modal.GetByLabel("Name", new() { Exact = true }).FillAsync(newName);
        await modal.GetByRole(AriaRole.Button, new() { Name = "Save changes" }).ClickAsync();

        await Expect(modal).ToBeHiddenAsync();
        await Expect(page.Locator("#property-list")).ToContainTextAsync(newName);
        await page.AssertNotReloadedAsync();
    }

    [Fact]
    public async Task RemoveProperty_TakesItOutOfTheList()
    {
        var propertyId = await NewPropertyIdAsync();
        var name = await _fixture.App.QueryDbAsync(db => db.Properties.Where(p => p.Id == propertyId).Select(p => p.Name).SingleAsync());
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;
        await page.GotoAsync("/Properties");
        await page.MarkPageAsync();
        var modal = page.Modal();

        await page.Locator($"[data-modal-url$='/Properties/Delete/{propertyId}']").ClickAsync();
        await Expect(modal).ToContainTextAsync(name);
        await modal.GetByRole(AriaRole.Button, new() { Name = "Remove", Exact = true }).ClickAsync();

        await Expect(modal).ToBeHiddenAsync();
        await Expect(page.Locator("#property-list")).Not.ToContainTextAsync(name);
        await page.AssertNotReloadedAsync();
    }

    [Fact]
    public async Task PropertyName_OpensDetails_AndBreadcrumbReturnsToList()
    {
        var propertyId = await NewPropertyIdAsync();
        var name = await _fixture.App.QueryDbAsync(db => db.Properties.Where(p => p.Id == propertyId).Select(p => p.Name).SingleAsync());
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;

        await page.GotoAsync("/Properties");
        await page.GetByRole(AriaRole.Link, new() { Name = name }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = name })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Navigation, new() { Name = "breadcrumb" }).GetByRole(AriaRole.Link, new() { Name = "Properties" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Properties", Exact = true })).ToBeVisibleAsync();
    }

    private async Task<int> NewPropertyIdAsync()
    {
        var unitId = await _fixture.App.CreateUnitAsync();
        return await _fixture.App.QueryDbAsync(db => db.Units.Where(u => u.Id == unitId).Select(u => u.PropertyId).SingleAsync());
    }
}
