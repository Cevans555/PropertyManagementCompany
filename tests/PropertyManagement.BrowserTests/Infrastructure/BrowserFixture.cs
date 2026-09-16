using Microsoft.Playwright;
using PropertyManagement.Data.Seeding;
using static Microsoft.Playwright.Assertions;

namespace PropertyManagement.BrowserTests.Infrastructure;

public sealed class BrowserFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;

    public KestrelWebFactory App { get; } = new();

    public IBrowser Browser { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
            throw new InvalidOperationException($"Installing the Playwright Chromium browser failed with exit code {exitCode}.");

        await App.InitializeAsync();
        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
            await Browser.DisposeAsync();

        _playwright?.Dispose();
        await ((IAsyncLifetime)App).DisposeAsync();
    }

    public async Task<(IBrowserContext Context, IPage Page)> SignInAsync(string email)
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions { BaseURL = App.BaseUrl });
        context.SetDefaultTimeout(15_000);
        var page = await context.NewPageAsync();

        await page.GotoAsync("/Account/Login");
        await page.GetByLabel("Email").FillAsync(email);
        await page.GetByLabel("Password").FillAsync(DbInitializer.DemoPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Log out" })).ToBeVisibleAsync();

        return (context, page);
    }
}
