using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using PropertyManagement.BrowserTests.Infrastructure;
using PropertyManagement.IntegrationTests.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace PropertyManagement.BrowserTests;

[Collection(BrowserCollection.Name)]
public class ApplicationGridTests
{
    private readonly BrowserFixture _fixture;

    public ApplicationGridTests(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PagesSortsAndFilters_WithoutReloading_AndKeepsStateInUrl()
    {
        var unitId = await _fixture.App.CreateUnitAsync();
        var applicantId = await _fixture.App.UserIdAsync(TestAccounts.Applicant);
        await _fixture.App.CreateApplicationAsync(unitId, applicantId, submit: false);
        await _fixture.App.CreateApplicationAsync(unitId, applicantId);
        await _fixture.App.CreateApplicationAsync(unitId, applicantId);
        var propertyId = await _fixture.App.QueryDbAsync(db =>
            db.Units.Where(u => u.Id == unitId).Select(u => u.PropertyId).SingleAsync());

        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;
        await page.GotoAsync($"/Applications?propertyId={propertyId}&pageSize=2");
        await page.MarkPageAsync();
        var rows = page.Locator("#application-grid tbody tr");
        var summary = page.Locator("#application-grid [data-grid-summary]");
        var next = page.GetByRole(AriaRole.Button, new() { Name = "Next" });

        await Expect(summary).ToHaveTextAsync("Showing 1–2 of 3");
        await Expect(rows).ToHaveCountAsync(2);
        await next.ClickAsync();
        await Expect(summary).ToHaveTextAsync("Showing 3–3 of 3");
        await Expect(rows).ToHaveCountAsync(1);
        await Expect(page).ToHaveURLAsync(new Regex("page=2"));
        await Expect(next).ToBeDisabledAsync();

        var statusHeader = page.Locator("th[data-grid-sort-key='status']");
        await statusHeader.GetByRole(AriaRole.Button).ClickAsync();
        await Expect(statusHeader).ToHaveAttributeAsync("aria-sort", "ascending");
        await Expect(summary).ToHaveTextAsync("Showing 1–2 of 3");
        await Expect(rows.First.Locator(".badge")).ToHaveTextAsync("Draft");
        await statusHeader.GetByRole(AriaRole.Button).ClickAsync();
        await Expect(statusHeader).ToHaveAttributeAsync("aria-sort", "descending");
        await Expect(rows.First.Locator(".badge")).ToHaveTextAsync("Submitted");

        await page.GetByLabel("Status").SelectOptionAsync("Draft");
        await page.GetByRole(AriaRole.Button, new() { Name = "Filter" }).ClickAsync();
        await Expect(summary).ToHaveTextAsync("Showing 1–1 of 1");
        await Expect(page.Locator("#application-grid [data-grid-pager]")).ToBeHiddenAsync();
        await Expect(page).ToHaveURLAsync(new Regex("status=Draft"));
        await page.AssertNotReloadedAsync();

        await page.GoBackAsync();
        await Expect(summary).ToHaveTextAsync("Showing 1–2 of 3");
        await Expect(page.GetByLabel("Status")).ToHaveValueAsync("");

        await page.ReloadAsync();
        await Expect(page.Locator("th[data-grid-sort-key='status']")).ToHaveAttributeAsync("aria-sort", "descending");
        await Expect(summary).ToHaveTextAsync("Showing 1–2 of 3");
    }
}
