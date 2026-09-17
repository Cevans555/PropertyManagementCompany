using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using PropertyManagement.BrowserTests.Infrastructure;
using PropertyManagement.IntegrationTests.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace PropertyManagement.BrowserTests;

[Collection(BrowserCollection.Name)]
public class ApplicationGridScopeTests
{
    private readonly BrowserFixture _fixture;

    public ApplicationGridScopeTests(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FilterWithNoMatches_ShowsEmptyState()
    {
        var unitId = await _fixture.App.CreateUnitAsync();
        await _fixture.App.CreateApplicationAsync(unitId, await _fixture.App.UserIdAsync(TestAccounts.Applicant));
        var propertyId = await PropertyIdAsync(unitId);
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;

        await page.GotoAsync($"/Applications?propertyId={propertyId}&status=Approved");

        await Expect(page.Locator("#application-grid tbody")).ToContainTextAsync("No applications match these filters.");
    }

    [Fact]
    public async Task Applicant_SeesOnlyTheirOwnRows_AndNoClaimedByColumn()
    {
        var unitId = await _fixture.App.CreateUnitAsync();
        var mine = await _fixture.App.CreateApplicationAsync(unitId, await _fixture.App.UserIdAsync(TestAccounts.Applicant));
        var theirs = await _fixture.App.CreateApplicationAsync(unitId, await _fixture.App.UserIdAsync(TestAccounts.OtherApplicant));
        var propertyId = await PropertyIdAsync(unitId);
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Applicant);
        await using var _ = context;

        await page.GotoAsync($"/Applications?propertyId={propertyId}");
        var grid = page.Locator("#application-grid");

        await Expect(grid.Locator($"a[href$='/Applications/Details/{mine}']")).ToHaveCountAsync(1);
        await Expect(grid.Locator($"a[href$='/Applications/Details/{theirs}']")).ToHaveCountAsync(0);
        await Expect(grid.Locator("th[data-grid-sort-key='claimedBy']")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task GridRowLink_OpensTheApplication()
    {
        var unitId = await _fixture.App.CreateUnitAsync();
        var applicationId = await _fixture.App.CreateApplicationAsync(unitId, await _fixture.App.UserIdAsync(TestAccounts.Applicant));
        var propertyId = await PropertyIdAsync(unitId);
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;

        await page.GotoAsync($"/Applications?propertyId={propertyId}");
        await page.Locator($"#application-grid a[href$='/Applications/Details/{applicationId}']").ClickAsync();

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Claim for review" })).ToBeVisibleAsync();
    }

    private Task<int> PropertyIdAsync(int unitId)
    {
        return _fixture.App.QueryDbAsync(db => db.Units.Where(u => u.Id == unitId).Select(u => u.PropertyId).SingleAsync());
    }
}
