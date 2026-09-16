using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.IntegrationTests.Infrastructure;

namespace PropertyManagement.IntegrationTests;

[Collection(WebCollection.Name)]
public class UnitModalTests
{
    private readonly PropertyManagementWebFactory _factory;

    public UnitModalTests(PropertyManagementWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateUnit_WithInactiveType_IsRejected()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var propertyId = await _factory.QueryDbAsync(db => db.Properties.Select(p => p.Id).FirstAsync());
        var inactiveTypeId = await InactiveUnitTypeIdAsync();

        var response = await PostUnitAsync(client, $"/Units/Create?propertyId={propertyId}", "999", inactiveTypeId);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("inactive", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreateUnitForm_DoesNotOfferInactiveTypes()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var propertyId = await _factory.QueryDbAsync(db => db.Properties.Select(p => p.Id).FirstAsync());

        var html = await client.GetStringAsync($"/Units/Create?propertyId={propertyId}");

        Assert.Contains("Studio", html);
        Assert.DoesNotContain("(inactive)", html);
    }

    [Fact]
    public async Task EditUnit_KeepingItsInactiveType_Succeeds()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var unit = await _factory.QueryDbAsync(db => db.Units.AsNoTracking().FirstAsync(u => !u.UnitType.IsActive));

        var form = await client.GetStringAsync($"/Units/Edit/{unit.Id}");
        var response = await PostUnitAsync(client, $"/Units/Edit/{unit.Id}", unit.UnitNumber, unit.UnitTypeId, rent: 2345m);

        Assert.Contains("(inactive)", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await _factory.QueryDbAsync(db => db.Units.AsNoTracking().SingleAsync(u => u.Id == unit.Id));
        Assert.Equal(2345m, saved.MonthlyRent);
        Assert.Equal(unit.UnitTypeId, saved.UnitTypeId);
    }

    [Fact]
    public async Task EditUnit_SwitchingToInactiveType_IsRejected()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var unit = await _factory.QueryDbAsync(db => db.Units.AsNoTracking().FirstAsync(u => u.UnitType.IsActive));

        var response = await PostUnitAsync(client, $"/Units/Edit/{unit.Id}", unit.UnitNumber, await InactiveUnitTypeIdAsync());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var saved = await _factory.QueryDbAsync(db => db.Units.AsNoTracking().SingleAsync(u => u.Id == unit.Id));
        Assert.Equal(unit.UnitTypeId, saved.UnitTypeId);
    }

    [Fact]
    public async Task CreateUnit_DuplicateNumber_IsRejected()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var existing = await _factory.QueryDbAsync(db => db.Units.AsNoTracking().FirstAsync());

        var response = await PostUnitAsync(
            client, $"/Units/Create?propertyId={existing.PropertyId}", existing.UnitNumber, await ActiveUnitTypeIdAsync());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("already exists", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DeleteUnit_WithApplications_IsRefused()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var unitId = await _factory.QueryDbAsync(db => db.RentalApplications.Select(a => a.UnitId).FirstAsync());

        var response = await client.PostFormAsync($"/Units/Delete/{unitId}", $"/Units/Delete/{unitId}", new());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.True(await _factory.QueryDbAsync(db => db.Units.AnyAsync(u => u.Id == unitId)));
    }

    [Fact]
    public async Task DeleteUnit_WithALease_IsRefused()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var unitId = await _factory.QueryDbAsync(db => db.Leases.Select(l => l.UnitId).FirstAsync());

        var response = await client.PostFormAsync($"/Units/Delete/{unitId}", $"/Units/Delete/{unitId}", new());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.True(await _factory.QueryDbAsync(db => db.Units.AnyAsync(u => u.Id == unitId)));
    }

    [Fact]
    public async Task CreateThenDeleteUnit_UpdatesUnitTable()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var propertyId = await _factory.QueryDbAsync(db => db.Properties.Select(p => p.Id).FirstAsync());
        var unitNumber = $"T{Random.Shared.Next(1000, 9999)}";

        var created = await PostUnitAsync(client, $"/Units/Create?propertyId={propertyId}", unitNumber, await ActiveUnitTypeIdAsync());
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        Assert.Contains(unitNumber, await client.GetStringAsync($"/Units/Table/{propertyId}"));

        var unitId = await _factory.QueryDbAsync(db =>
            db.Units.Where(u => u.PropertyId == propertyId && u.UnitNumber == unitNumber).Select(u => u.Id).SingleAsync());
        var deleted = await client.PostFormAsync($"/Units/Delete/{unitId}", $"/Units/Delete/{unitId}", new());

        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.DoesNotContain(unitNumber, await client.GetStringAsync($"/Units/Table/{propertyId}"));
    }

    [Fact]
    public async Task CreateUnit_PostedByApplicant_IsDenied()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Applicant);
        var propertyId = await _factory.QueryDbAsync(db => db.Properties.Select(p => p.Id).FirstAsync());

        var response = await client.PostFormAsync("/", $"/Units/Create?propertyId={propertyId}", new()
        {
            ["UnitNumber"] = "X1",
            ["Bedrooms"] = "1",
            ["MonthlyRent"] = "1000",
            ["UnitTypeId"] = (await ActiveUnitTypeIdAsync()).ToString(CultureInfo.InvariantCulture)
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.OriginalString);
        Assert.False(await _factory.QueryDbAsync(db => db.Units.AnyAsync(u => u.PropertyId == propertyId && u.UnitNumber == "X1")));
    }

    private Task<int> InactiveUnitTypeIdAsync()
    {
        return _factory.QueryDbAsync(db => db.UnitTypes.Where(t => !t.IsActive).Select(t => t.Id).FirstAsync());
    }

    private Task<int> ActiveUnitTypeIdAsync()
    {
        return _factory.QueryDbAsync(db => db.UnitTypes.Where(t => t.IsActive).Select(t => t.Id).FirstAsync());
    }

    private static Task<HttpResponseMessage> PostUnitAsync(
        HttpClient client, string url, string unitNumber, int unitTypeId, decimal rent = 1500m)
    {
        return client.PostFormAsync(url, url, new()
        {
            ["UnitNumber"] = unitNumber,
            ["Bedrooms"] = "2",
            ["MonthlyRent"] = rent.ToString(CultureInfo.InvariantCulture),
            ["UnitTypeId"] = unitTypeId.ToString(CultureInfo.InvariantCulture)
        });
    }
}
