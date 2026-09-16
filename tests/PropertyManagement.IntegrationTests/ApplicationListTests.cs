using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.IntegrationTests.Infrastructure;

namespace PropertyManagement.IntegrationTests;

[Collection(WebCollection.Name)]
public class ApplicationListTests
{
    private readonly PropertyManagementWebFactory _factory;

    public ApplicationListTests(PropertyManagementWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Page_RedirectsAnonymousUserToLogin()
    {
        var response = await _factory.CreateBrowserClient().GetAsync("/Applications");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Page_RendersGridWithClaimedByColumnOnlyForManagers()
    {
        var applicantClient = await _factory.CreateSignedInClientAsync(TestAccounts.Applicant);
        var managerClient = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var applicant = await applicantClient.GetStringAsync("/Applications");
        var manager = await managerClient.GetStringAsync("/Applications");

        Assert.Contains("data-grid-url=\"/api/applications\"", applicant);
        Assert.Contains("js/grid.js", applicant);
        Assert.DoesNotContain("Claimed by", applicant);
        Assert.Contains("Claimed by", manager);
    }

    [Fact]
    public async Task Page_PreselectsFilterFromQueryString_AndIgnoresInvalidValues()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var html = await client.GetStringAsync("/Applications?status=Submitted");
        var invalid = await client.GetAsync("/Applications?status=NotAStatus");

        Assert.Contains("<option selected=\"selected\" value=\"Submitted\">", html);
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
    }

    [Fact]
    public async Task Api_Anonymous_Returns401InsteadOfLoginRedirect()
    {
        var response = await _factory.CreateBrowserClient().GetAsync("/api/applications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Api_Applicant_GetsOnlyApplicationsTheyAreOn()
    {
        var unitId = await _factory.CreateUnitAsync();
        var own = await CreateApplicationAsync(unitId, TestAccounts.Applicant);
        await CreateApplicationAsync(unitId, TestAccounts.OtherApplicant);
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Applicant);

        var page = await GetPageAsync(client, $"propertyId={await PropertyIdAsync(unitId)}");

        Assert.Equal(1, page.TotalCount);
        Assert.Equal([own], page.Ids);
    }

    [Fact]
    public async Task Api_Manager_GetsEveryonesApplications_WithClaimedBy()
    {
        var unitId = await _factory.CreateUnitAsync();
        var managerId = await _factory.UserIdAsync(TestAccounts.Manager);
        var claimed = await CreateApplicationAsync(unitId, TestAccounts.Applicant, claimedBy: managerId);
        var other = await CreateApplicationAsync(unitId, TestAccounts.OtherApplicant);
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var page = await GetPageAsync(client, $"propertyId={await PropertyIdAsync(unitId)}");

        Assert.Equal(2, page.TotalCount);
        Assert.Equal([claimed, other], page.Ids.Order());
        Assert.NotNull(page.Item(claimed).GetProperty("claimedBy").GetString());
        Assert.Equal("UnderReview", page.Item(claimed).GetProperty("status").GetString());
        Assert.Equal("Under Review", page.Item(claimed).GetProperty("statusName").GetString());
        Assert.Equal($"/Applications/Details/{claimed}", page.Item(claimed).GetProperty("detailsUrl").GetString());
    }

    [Fact]
    public async Task Api_Applicant_NeverSeesWhoClaimedTheirApplication()
    {
        var unitId = await _factory.CreateUnitAsync();
        var managerId = await _factory.UserIdAsync(TestAccounts.Manager);
        var claimed = await CreateApplicationAsync(unitId, TestAccounts.Applicant, claimedBy: managerId);
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Applicant);

        var page = await GetPageAsync(client, $"propertyId={await PropertyIdAsync(unitId)}&sort=claimedBy");

        Assert.Equal(JsonValueKind.Null, page.Item(claimed).GetProperty("claimedBy").ValueKind);
    }

    [Fact]
    public async Task Api_StatusFilter_CountsOnlyMatchingApplications()
    {
        var unitId = await _factory.CreateUnitAsync();
        var draft = await CreateApplicationAsync(unitId, TestAccounts.Applicant, submit: false);
        await CreateApplicationAsync(unitId, TestAccounts.OtherApplicant, submit: true);
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var page = await GetPageAsync(client, $"propertyId={await PropertyIdAsync(unitId)}&status=Draft");

        Assert.Equal(1, page.TotalCount);
        Assert.Equal([draft], page.Ids);
    }

    [Fact]
    public async Task Api_PagesThroughFilteredResults_WithTotalAcrossPages()
    {
        var unitId = await _factory.CreateUnitAsync();
        var ids = new List<int>();
        for (var i = 0; i < 5; i++)
        {
            ids.Add(await CreateApplicationAsync(unitId, TestAccounts.Applicant));
        }

        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var filter = $"propertyId={await PropertyIdAsync(unitId)}&pageSize=2";

        var first = await GetPageAsync(client, $"{filter}&page=1");
        var second = await GetPageAsync(client, $"{filter}&page=2");
        var third = await GetPageAsync(client, $"{filter}&page=3");

        Assert.All([first, second, third], p => Assert.Equal(5, p.TotalCount));
        Assert.Equal([2, 2, 1], [first.Ids.Count, second.Ids.Count, third.Ids.Count]);
        Assert.Equal(ids.AsEnumerable().Reverse(), first.Ids.Concat(second.Ids).Concat(third.Ids));
    }

    [Fact]
    public async Task Api_PagePastTheEnd_IsClampedToLastPage()
    {
        var unitId = await _factory.CreateUnitAsync();
        for (var i = 0; i < 3; i++)
        {
            await CreateApplicationAsync(unitId, TestAccounts.Applicant);
        }

        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var page = await GetPageAsync(client, $"propertyId={await PropertyIdAsync(unitId)}&pageSize=2&page=50");

        Assert.Equal(2, page.Page);
        Assert.Single(page.Ids);
    }

    [Fact]
    public async Task Api_SortsByStatusInBothDirections()
    {
        var unitId = await _factory.CreateUnitAsync();
        var draft = await CreateApplicationAsync(unitId, TestAccounts.Applicant, submit: false);
        var submitted = await CreateApplicationAsync(unitId, TestAccounts.OtherApplicant, submit: true);
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var filter = $"propertyId={await PropertyIdAsync(unitId)}&sort=status";

        var ascending = await GetPageAsync(client, $"{filter}&direction=asc");
        var descending = await GetPageAsync(client, $"{filter}&direction=desc");

        Assert.Equal([draft, submitted], ascending.Ids);
        Assert.Equal([submitted, draft], descending.Ids);
    }

    [Fact]
    public async Task Api_SortsByProperty()
    {
        var firstUnit = await _factory.CreateUnitAsync();
        var secondUnit = await _factory.CreateUnitAsync();
        await CreateApplicationAsync(firstUnit, TestAccounts.Applicant);
        await CreateApplicationAsync(secondUnit, TestAccounts.Applicant);
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var page = await GetPageAsync(client, "sort=property&direction=asc&pageSize=100");

        var names = page.Items.Select(i => i.GetProperty("propertyName").GetString()!).ToList();
        Assert.Equal(names.Order(StringComparer.OrdinalIgnoreCase), names);
    }

    [Theory]
    [InlineData("sort=applicants")]
    [InlineData("direction=sideways")]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=0")]
    [InlineData("page=0")]
    [InlineData("status=NotAStatus")]
    public async Task Api_InvalidParameter_Returns400ProblemDetails(string query)
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var response = await client.GetAsync($"/api/applications?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task OpenApiDocument_DescribesTheListEndpoint()
    {
        var response = await _factory.CreateBrowserClient().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var operation = document.RootElement.GetProperty("paths").GetProperty("/api/applications").GetProperty("get");

        Assert.Equal("ListApplications", operation.GetProperty("operationId").GetString());

        var parameters = operation.GetProperty("parameters").EnumerateArray()
            .Select(p => p.GetProperty("name").GetString())
            .ToHashSet();

        Assert.Superset(new HashSet<string?>(["status", "propertyId", "page", "pageSize", "sort", "direction"]), parameters);
        Assert.Contains("totalCount", document.RootElement.GetProperty("components").GetRawText());
    }

    private async Task<int> CreateApplicationAsync(int unitId, string applicantEmail, bool submit = true, string? claimedBy = null)
    {
        return await _factory.CreateApplicationAsync(unitId, await _factory.UserIdAsync(applicantEmail), submit, claimedBy);
    }

    private Task<int> PropertyIdAsync(int unitId)
    {
        return _factory.QueryDbAsync(db => db.Units.Where(u => u.Id == unitId).Select(u => u.PropertyId).SingleAsync());
    }

    private static async Task<ApiPage> GetPageAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync($"/api/applications?{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        return new ApiPage(
            root.GetProperty("items").EnumerateArray().Select(i => i.Clone()).ToList(),
            root.GetProperty("totalCount").GetInt32(),
            root.GetProperty("page").GetInt32());
    }

    private sealed record ApiPage(IReadOnlyList<JsonElement> Items, int TotalCount, int Page)
    {
        public List<int> Ids => Items.Select(i => i.GetProperty("id").GetInt32()).ToList();

        public JsonElement Item(int id)
        {
            return Items.Single(i => i.GetProperty("id").GetInt32() == id);
        }
    }
}
