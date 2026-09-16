using System.Net;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.IntegrationTests.Infrastructure;

namespace PropertyManagement.IntegrationTests;

[Collection(WebCollection.Name)]
public class PropertiesControllerTests
{
    private readonly PropertyManagementWebFactory _factory;

    public PropertiesControllerTests(PropertyManagementWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Index_RedirectsAnonymousUserToLogin()
    {
        var client = _factory.CreateBrowserClient();

        var response = await client.GetAsync("/Properties");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Index_DeniesApplicant()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Applicant);

        var response = await client.GetAsync("/Properties");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Index_ShowsPropertiesToManager()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var seededName = await _factory.QueryDbAsync(db => db.Properties.OrderBy(p => p.Name).Select(p => p.Name).FirstAsync());

        var html = await client.GetStringAsync("/Properties");

        Assert.Contains("Add property", html);
        Assert.Contains(WebUtility.HtmlEncode(seededName), html);
    }

    [Fact]
    public async Task CreateModal_ReturnsFormPartialWithoutLayout()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var html = await client.GetStringAsync("/Properties/Create");

        Assert.Contains("<form", html);
        Assert.Contains("__RequestVerificationToken", html);
        Assert.DoesNotContain("<html", html);
    }

    [Fact]
    public async Task Create_InvalidForm_Returns422WithErrorsInPartial()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var response = await client.PostFormAsync("/Properties/Create", "/Properties/Create", new()
        {
            ["Name"] = "",
            ["Street"] = "1 Main St",
            ["City"] = "Albany",
            ["State"] = "NY",
            ["PostalCode"] = "12207"
        });
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("The Name field is required.", html);
        Assert.DoesNotContain("<html", html);
    }

    [Fact]
    public async Task Create_ValidForm_ReturnsSuccessAndSavesWithAudit()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var name = TestHelpers.UniqueName("Created");

        var response = await PostPropertyAsync(client, "/Properties/Create", name, "1 Main St");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"success\":true", await response.Content.ReadAsStringAsync());

        var managerId = await _factory.UserIdAsync(TestAccounts.Manager);
        var saved = await _factory.QueryDbAsync(db => db.Properties.AsNoTracking().SingleAsync(p => p.Name == name));
        Assert.Equal(managerId, saved.CreatedById);
        Assert.True(saved.CreatedAt > DateTime.UtcNow.AddMinutes(-5));
        Assert.Null(saved.ModifiedAt);

        var list = await client.GetStringAsync("/Properties/List");
        Assert.Contains(name, list);
    }

    [Fact]
    public async Task Edit_AddressOnlyChange_StampsModifiedAudit()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var otherClient = await _factory.CreateSignedInClientAsync(TestAccounts.OtherManager);
        var name = TestHelpers.UniqueName("Edited");
        await PostPropertyAsync(client, "/Properties/Create", name, "1 Main St");
        var id = await _factory.QueryDbAsync(db => db.Properties.Where(p => p.Name == name).Select(p => p.Id).SingleAsync());

        var response = await PostPropertyAsync(otherClient, $"/Properties/Edit/{id}", name, "99 New Street");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var otherManagerId = await _factory.UserIdAsync(TestAccounts.OtherManager);
        var saved = await _factory.QueryDbAsync(db => db.Properties.AsNoTracking().SingleAsync(p => p.Id == id));
        Assert.Equal("99 New Street", saved.Address.Street);
        Assert.NotNull(saved.ModifiedAt);
        Assert.Equal(otherManagerId, saved.ModifiedById);
    }

    [Fact]
    public async Task Post_WithoutAntiforgeryToken_IsRejected()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var response = await client.PostAsync("/Properties/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Name"] = TestHelpers.UniqueName("NoToken"),
            ["Street"] = "1 Main St",
            ["City"] = "Albany",
            ["State"] = "NY",
            ["PostalCode"] = "12207"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_PropertyWithApplications_IsRefused()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var id = await _factory.QueryDbAsync(db =>
            db.Properties.Where(p => p.Units.Any(u => db.RentalApplications.Any(a => a.UnitId == u.Id))).Select(p => p.Id).FirstAsync());

        var response = await client.PostFormAsync($"/Properties/Delete/{id}", $"/Properties/Delete/{id}", new());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("can&#x27;t be removed", await response.Content.ReadAsStringAsync());
        Assert.True(await _factory.QueryDbAsync(db => db.Properties.AnyAsync(p => p.Id == id)));
    }

    [Fact]
    public async Task Delete_UnusedProperty_RemovesItAndItsUnits()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var name = TestHelpers.UniqueName("Removed");
        await PostPropertyAsync(client, "/Properties/Create", name, "1 Main St");
        var id = await _factory.QueryDbAsync(db => db.Properties.Where(p => p.Name == name).Select(p => p.Id).SingleAsync());

        var response = await client.PostFormAsync($"/Properties/Delete/{id}", $"/Properties/Delete/{id}", new());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await _factory.QueryDbAsync(db => db.Properties.AnyAsync(p => p.Id == id)));
    }

    private static Task<HttpResponseMessage> PostPropertyAsync(HttpClient client, string url, string name, string street)
    {
        return client.PostFormAsync(url, url, new()
        {
            ["Name"] = name,
            ["Street"] = street,
            ["City"] = "Albany",
            ["State"] = "NY",
            ["PostalCode"] = "12207"
        });
    }
}
