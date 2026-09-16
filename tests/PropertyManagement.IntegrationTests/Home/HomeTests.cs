using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Enums;
using PropertyManagement.Data.Seeding;
using PropertyManagement.IntegrationTests.Infrastructure;

namespace PropertyManagement.IntegrationTests;

[Collection(WebCollection.Name)]
public class HomeTests
{
    private readonly PropertyManagementWebFactory _factory;

    public HomeTests(PropertyManagementWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Home_ForAVisitor_ExplainsTheAppWithoutLeakingDemoCredentials()
    {
        var client = _factory.CreateBrowserClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("Rental applications, start to finish", html);
        Assert.Contains("Create an account", html);

        // The demo sign-in card is only rendered in Development. The test host runs as "Testing", so a deployed
        // environment would not show it either.
        Assert.DoesNotContain(DbInitializer.DemoPassword, html);
        Assert.DoesNotContain("Demo sign-ins", html);
        Assert.DoesNotContain(TestAccounts.Manager, html);
    }

    [Fact]
    public async Task Home_ForAManager_ShowsTheReviewWorkload()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var managerId = await _factory.UserIdAsync(TestAccounts.Manager);

        var expectedWaiting = await _factory.QueryDbAsync(db =>
            db.RentalApplications.CountAsync(a => a.Status == ApplicationStatus.Submitted));
        var expectedClaimed = await _factory.QueryDbAsync(db =>
            db.RentalApplications.CountAsync(a => a.Status == ApplicationStatus.UnderReview && a.ClaimedById == managerId));
        var expectedProperties = await _factory.QueryDbAsync(db => db.Properties.CountAsync());

        var html = await client.GetStringAsync("/");

        Assert.Contains("property manager", html);
        Assert.Contains("Waiting to be claimed", html);
        Assert.Contains("Go to review queue", html);
        AssertCount(html, expectedWaiting, "Waiting to be claimed");
        AssertCount(html, expectedClaimed, "Claimed by you");
        AssertCount(html, expectedProperties, "Properties");

        // An applicant's dashboard must not be what a manager sees.
        Assert.DoesNotContain("Need your attention", html);
    }

    [Fact]
    public async Task Home_ForAnApplicant_CountsOnlyTheirOwnApplications()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Applicant);
        var userId = await _factory.UserIdAsync(TestAccounts.Applicant);

        var expectedAwaiting = await _factory.QueryDbAsync(db => db.RentalApplications
            .Where(a => a.Applicants.Any(p => p.UserId == userId))
            .CountAsync(a => a.Status == ApplicationStatus.Submitted || a.Status == ApplicationStatus.UnderReview));

        var html = await client.GetStringAsync("/");

        Assert.Contains("applicant", html);
        Assert.Contains("Waiting on a decision", html);
        AssertCount(html, expectedAwaiting, "Waiting on a decision");

        // Nothing from the manager dashboard should reach an applicant.
        Assert.DoesNotContain("Waiting to be claimed", html);
        Assert.DoesNotContain("Go to review queue", html);
    }

    [Fact]
    public async Task Home_ApplicantCount_FollowsTheirOwnApplications()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Applicant);
        var userId = await _factory.UserIdAsync(TestAccounts.Applicant);

        var before = AwaitingDecisionCount(await client.GetStringAsync("/"));

        var unitId = await _factory.CreateUnitAsync();
        await _factory.CreateApplicationAsync(unitId, userId);

        Assert.Equal(before + 1, AwaitingDecisionCount(await client.GetStringAsync("/")));
    }

    private static void AssertCount(string html, int expected, string label)
    {
        Assert.Contains($"<div class=\"display-6\">{expected}</div>", html);
        Assert.Contains(label, html);
    }

    private static int AwaitingDecisionCount(string html)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            html, "<div class=\"display-6\">(\\d+)</div>\\s*<div class=\"text-muted\">Waiting on a decision</div>");
        Assert.True(match.Success, "The applicant dashboard did not render a 'Waiting on a decision' count.");
        return int.Parse(match.Groups[1].Value);
    }
}
