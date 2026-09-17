using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using PropertyManagement.BrowserTests.Infrastructure;
using PropertyManagement.IntegrationTests.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace PropertyManagement.BrowserTests;

[Collection(BrowserCollection.Name)]
public class UnitModalTests
{
    private readonly BrowserFixture _fixture;

    public UnitModalTests(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddUnit_DuplicateNumberErrorRendersInModal_ThenSucceeds()
    {
        var propertyId = await PropertyOfNewUnitAsync();
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;
        await page.GotoAsync($"/Properties/Details/{propertyId}");
        await page.MarkPageAsync();
        var modal = page.Modal();

        await page.GetByRole(AriaRole.Button, new() { Name = "Add unit" }).ClickAsync();
        await modal.GetByLabel("Unit number").FillAsync("101");
        await modal.GetByLabel("Unit type").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await modal.GetByLabel("Bedrooms").FillAsync("2");
        await modal.GetByLabel("Monthly rent").FillAsync("1500");
        await modal.GetByRole(AriaRole.Button, new() { Name = "Add unit" }).ClickAsync();

        await Expect(modal).ToContainTextAsync("already exists");
        await Expect(modal).ToBeVisibleAsync();

        await modal.GetByLabel("Unit number").FillAsync("102");
        await modal.GetByRole(AriaRole.Button, new() { Name = "Add unit" }).ClickAsync();

        await Expect(modal).ToBeHiddenAsync();
        await Expect(page.Locator("#unit-table")).ToContainTextAsync("102");
        await page.AssertNotReloadedAsync();
    }

    [Fact]
    public async Task EditUnit_ChangesRent_AndRefreshesTableWithoutReload()
    {
        var unitId = await _fixture.App.CreateUnitAsync();
        var propertyId = await PropertyIdAsync(unitId);
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;
        await page.GotoAsync($"/Properties/Details/{propertyId}");
        await page.MarkPageAsync();
        var modal = page.Modal();

        await page.Locator($"[data-modal-url$='/Units/Edit/{unitId}']").ClickAsync();
        await Expect(modal.GetByRole(AriaRole.Heading, new() { Name = "Edit unit 101" })).ToBeVisibleAsync();
        await modal.GetByLabel("Monthly rent").FillAsync("1875");
        await modal.GetByRole(AriaRole.Button, new() { Name = "Save changes" }).ClickAsync();

        await Expect(modal).ToBeHiddenAsync();
        await Expect(page.Locator("#unit-table")).ToContainTextAsync("$1,875");
        await page.AssertNotReloadedAsync();
    }

    [Fact]
    public async Task EditUnit_WithInactiveType_ShowsItAsTheCurrentChoiceOnly()
    {
        var unitId = await _fixture.App.QueryDbAsync(db =>
            db.Units.Where(u => !u.UnitType.IsActive).Select(u => u.Id).FirstAsync());
        var propertyId = await PropertyIdAsync(unitId);
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;
        await page.GotoAsync($"/Properties/Details/{propertyId}");
        var modal = page.Modal();

        await page.Locator($"[data-modal-url$='/Units/Edit/{unitId}']").ClickAsync();
        await Expect(modal.GetByLabel("Unit type").Locator("option:checked")).ToContainTextAsync("(inactive)");
        await modal.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await Expect(modal).ToBeHiddenAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Add unit" }).ClickAsync();
        await Expect(modal.GetByLabel("Unit type").Locator("option", new() { HasText = "(inactive)" })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task RemoveUnit_ConfirmationModal_ShowsEmptyState()
    {
        var unitId = await _fixture.App.CreateUnitAsync();
        var propertyId = await PropertyIdAsync(unitId);
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;
        await page.GotoAsync($"/Properties/Details/{propertyId}");
        await page.MarkPageAsync();
        var modal = page.Modal();

        await page.Locator($"[data-modal-url$='/Units/Delete/{unitId}']").ClickAsync();
        await Expect(modal.GetByRole(AriaRole.Heading, new() { Name = "Remove unit" })).ToBeVisibleAsync();
        await modal.GetByRole(AriaRole.Button, new() { Name = "Remove", Exact = true }).ClickAsync();

        await Expect(modal).ToBeHiddenAsync();
        await Expect(page.Locator("#unit-table")).ToContainTextAsync("This property has no units yet.");
        await page.AssertNotReloadedAsync();
    }

    [Fact]
    public async Task RemoveUnit_WithApplications_IsRefusedInsideTheModal()
    {
        var unitId = await _fixture.App.CreateUnitAsync();
        await _fixture.App.CreateApplicationAsync(unitId, await _fixture.App.UserIdAsync(TestAccounts.Applicant), submit: false);
        var propertyId = await PropertyIdAsync(unitId);
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;
        await page.GotoAsync($"/Properties/Details/{propertyId}");
        var modal = page.Modal();

        await page.Locator($"[data-modal-url$='/Units/Delete/{unitId}']").ClickAsync();
        await modal.GetByRole(AriaRole.Button, new() { Name = "Remove", Exact = true }).ClickAsync();

        await Expect(modal).ToContainTextAsync("can't be removed");
        await Expect(modal).ToBeVisibleAsync();
    }

    private async Task<int> PropertyOfNewUnitAsync()
    {
        return await PropertyIdAsync(await _fixture.App.CreateUnitAsync());
    }

    private Task<int> PropertyIdAsync(int unitId)
    {
        return _fixture.App.QueryDbAsync(db => db.Units.Where(u => u.Id == unitId).Select(u => u.PropertyId).SingleAsync());
    }
}
