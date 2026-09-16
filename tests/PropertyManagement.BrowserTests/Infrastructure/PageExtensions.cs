using Microsoft.Playwright;

namespace PropertyManagement.BrowserTests.Infrastructure;

public static class PageExtensions
{
    public static Task MarkPageAsync(this IPage page)
    {
        return page.EvaluateAsync("() => { window.__notReloaded = true; }");
    }

    public static async Task AssertNotReloadedAsync(this IPage page)
    {
        var marked = await page.EvaluateAsync<bool>("() => window.__notReloaded === true");
        Assert.True(marked, "The page reloaded; expected only a section to refresh.");
    }

    public static ILocator Modal(this IPage page)
    {
        return page.Locator("#app-modal");
    }
}
