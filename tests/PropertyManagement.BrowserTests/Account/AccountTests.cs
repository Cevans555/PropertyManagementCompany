using System.Text.RegularExpressions;
using Microsoft.Playwright;
using PropertyManagement.BrowserTests.Infrastructure;
using PropertyManagement.Data.Seeding;
using PropertyManagement.IntegrationTests.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace PropertyManagement.BrowserTests;

[Collection(BrowserCollection.Name)]
public class AccountTests
{
    private readonly BrowserFixture _fixture;

    public AccountTests(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RegisterFromHomeRoleCard_PreselectsRole_AndSignsInAsApplicant()
    {
        var (context, page) = await _fixture.NewPageAsync();
        await using var _ = context;

        await page.GotoAsync("/");
        await page.GetByRole(AriaRole.Link, new() { Name = "Register as applicant" }).ClickAsync();
        await Expect(page.GetByLabel("Applicant")).ToBeCheckedAsync();

        await FillRegistrationAsync(page, $"browser-{Guid.NewGuid():N}@test.local");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Log out" })).ToBeVisibleAsync();
        await Expect(page.Locator("#main-nav").GetByRole(AriaRole.Link, new() { Name = "Available units", Exact = true })).ToBeVisibleAsync();
        await Expect(page.Locator("#main-nav").GetByRole(AriaRole.Link, new() { Name = "Review queue", Exact = true })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task RegisterAsManager_ShowsManagerNavigation()
    {
        var (context, page) = await _fixture.NewPageAsync();
        await using var _ = context;

        await page.GotoAsync("/Account/Register");
        await page.GetByLabel("Property manager").CheckAsync();
        await FillRegistrationAsync(page, $"browser-{Guid.NewGuid():N}@test.local");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();

        await Expect(page.Locator("#main-nav").GetByRole(AriaRole.Link, new() { Name = "Review queue", Exact = true })).ToBeVisibleAsync();
        await Expect(page.Locator("#main-nav").GetByRole(AriaRole.Link, new() { Name = "Properties", Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Register_WithMismatchedPasswords_IsCaughtInTheBrowser()
    {
        var (context, page) = await _fixture.NewPageAsync();
        await using var _ = context;
        var posts = 0;
        page.Request += (_, request) =>
        {
            if (request.Method == "POST")
                posts++;
        };

        await page.GotoAsync("/Account/Register");
        await page.GetByLabel("Applicant").CheckAsync();
        await FillRegistrationAsync(page, $"browser-{Guid.NewGuid():N}@test.local");
        await page.GetByLabel("Confirm password").FillAsync("Different123!");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();

        await Expect(page.Locator("[data-valmsg-for='ConfirmPassword']")).Not.ToBeEmptyAsync();
        Assert.Equal(0, posts);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShowsError_ThenLogInAndOut()
    {
        var (context, page) = await _fixture.NewPageAsync();
        await using var _ = context;

        await page.GotoAsync("/Account/Login");
        await page.GetByLabel("Email").FillAsync(TestAccounts.Applicant);
        await page.GetByLabel("Password").FillAsync("WrongPassword1!");
        await page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        await Expect(page.GetByText("Invalid email or password.")).ToBeVisibleAsync();

        await page.GetByLabel("Password").FillAsync(DbInitializer.DemoPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Welcome back" })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Log out" }).ClickAsync();
        await Expect(page.Locator("#main-nav").GetByRole(AriaRole.Link, new() { Name = "Log in", Exact = true })).ToBeVisibleAsync();
        await Expect(page).ToHaveURLAsync(new Regex("/$"));
    }

    [Fact]
    public async Task ProtectedPage_RedirectsToLogin_ThenReturnsThereAfterSignIn()
    {
        var (context, page) = await _fixture.NewPageAsync();
        await using var _ = context;

        await page.GotoAsync("/Reviews/Queue");
        await Expect(page).ToHaveURLAsync(new Regex("/Account/Login"));

        await page.GetByLabel("Email").FillAsync(TestAccounts.Manager);
        await page.GetByLabel("Password").FillAsync(DbInitializer.DemoPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        await Expect(page).ToHaveURLAsync(new Regex("/Reviews/Queue"));
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Review queue" })).ToBeVisibleAsync();
    }

    private static async Task FillRegistrationAsync(IPage page, string email)
    {
        await page.GetByLabel("First name").FillAsync("Browser");
        await page.GetByLabel("Last name").FillAsync("Tester");
        await page.GetByLabel("Email").FillAsync(email);
        await page.GetByLabel("Password", new() { Exact = true }).FillAsync("Password123!");
        await page.GetByLabel("Confirm password").FillAsync("Password123!");
    }
}
