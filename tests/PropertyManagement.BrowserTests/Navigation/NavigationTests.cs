using Microsoft.Playwright;
using PropertyManagement.BrowserTests.Infrastructure;
using PropertyManagement.Data.Seeding;
using PropertyManagement.IntegrationTests.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace PropertyManagement.BrowserTests;

[Collection(BrowserCollection.Name)]
public class NavigationTests
{
    private readonly BrowserFixture _fixture;

    public NavigationTests(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ApplicantNavigation_MarksTheCurrentPage_AndHidesManagerLinks()
    {
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Applicant);
        await using var _ = context;
        var nav = page.Locator("#main-nav");

        await nav.GetByRole(AriaRole.Link, new() { Name = "Available units" }).ClickAsync();
        await Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Available units" })).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("active"));

        await nav.GetByRole(AriaRole.Link, new() { Name = "My applications" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "My applications" })).ToBeVisibleAsync();

        await Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Properties" })).ToHaveCountAsync(0);
        await Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Review queue" })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task ManagerNavigation_ReachesEveryManagerPage()
    {
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;
        var nav = page.Locator("#main-nav");

        await nav.GetByRole(AriaRole.Link, new() { Name = "Applications" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Applications", Exact = true })).ToBeVisibleAsync();

        await nav.GetByRole(AriaRole.Link, new() { Name = "Review queue" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Review queue" })).ToBeVisibleAsync();

        await nav.GetByRole(AriaRole.Link, new() { Name = "Properties" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Properties", Exact = true })).ToBeVisibleAsync();

        await Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Available units" })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Applicant_OpeningAManagerPage_SeesAccessDenied()
    {
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Applicant);
        await using var _ = context;

        await page.GotoAsync("/Properties");

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Access denied" })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Go to the home page" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Welcome back" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ManagerDashboard_LinksToTheReviewQueue()
    {
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Manager);
        await using var _ = context;

        await page.GotoAsync("/");
        await Expect(page.GetByText("Waiting to be claimed")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Go to review queue" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Review queue" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task OnAPhone_TheMenuCollapses_AndOpensFromTheToggle()
    {
        var (context, page) = await _fixture.NewPageAsync(width: 375, height: 740);
        await using var _ = context;

        await page.GotoAsync("/Account/Login");
        await page.GetByLabel("Email").FillAsync(TestAccounts.Manager);
        await page.GetByLabel("Password").FillAsync(DbInitializer.DemoPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Toggle navigation" });
        var reviewQueue = page.Locator("#main-nav").GetByRole(AriaRole.Link, new() { Name = "Review queue" });
        await Expect(toggle).ToBeVisibleAsync();
        await Expect(reviewQueue).ToBeHiddenAsync();

        await toggle.ClickAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await reviewQueue.ClickAsync();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Review queue" })).ToBeVisibleAsync();
        var scrollWidth = await page.EvaluateAsync<int>("() => document.documentElement.scrollWidth");
        Assert.True(scrollWidth <= 375, $"The page scrolls sideways on a phone ({scrollWidth}px wide).");
    }
}
