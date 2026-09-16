using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Dtos;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.ValueObjects;
using PropertyManagement.Data;
using PropertyManagement.Data.Seeding;

namespace PropertyManagement.IntegrationTests.Infrastructure;

public static partial class TestHelpers
{
    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryTokenPattern();

    public static async Task<HttpClient> CreateSignedInClientAsync(this PropertyManagementWebFactory factory, string email)
    {
        var client = factory.CreateBrowserClient();
        var response = await client.PostFormAsync("/Account/Login", "/Account/Login", new()
        {
            ["Email"] = email,
            ["Password"] = DbInitializer.DemoPassword
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    public static async Task<HttpResponseMessage> PostFormAsync(
        this HttpClient client, string tokenPageUrl, string postUrl, Dictionary<string, string> fields)
    {
        var html = await client.GetStringAsync(tokenPageUrl);
        return await client.PostFormWithTokenAsync(html, postUrl, fields);
    }

    public static Task<HttpResponseMessage> PostFormWithTokenAsync(
        this HttpClient client, string html, string postUrl, Dictionary<string, string> fields)
    {
        var form = new Dictionary<string, string>(fields) { ["__RequestVerificationToken"] = AntiforgeryToken(html) };
        return client.PostAsync(postUrl, new FormUrlEncodedContent(form));
    }

    public static string AntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenPattern().Match(html);
        Assert.True(match.Success, "No antiforgery token found in the page.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    public static string HiddenValue(string html, string name)
    {
        var match = Regex.Match(html, $"name=\"{Regex.Escape(name)}\"[^>]*?value=\"([^\"]*)\"");
        Assert.True(match.Success, $"No input named {name} found in the page.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    public static Task<int> CreateApplicationAsync(
        this PropertyManagementWebFactory factory, int unitId, string applicantId, bool submit = true, string? claimedBy = null)
    {
        return factory.QueryDbAsync(async db =>
        {
            var now = DateTime.UtcNow;
            var application = RentalApplication.Start(unitId, applicantId, unitHasActiveLease: false, now);

            application.SaveApplicantDetails(applicantId, applicantId, ApplicantDetails(), now);
            application.AddResidence(applicantId, Residence());
            application.SaveResidenceSection(applicantId, now);

            if (submit)
                application.Submit(applicantId, unitHasActiveLease: false, now);

            if (claimedBy is not null)
                application.Claim(claimedBy, now);

            db.RentalApplications.Add(application);
            await db.SaveChangesAsync();
            return application.Id;
        });
    }

    public static Task<int> CreateUnitAsync(this PropertyManagementWebFactory factory)
    {
        return factory.QueryDbAsync(async db =>
        {
            var unitType = await db.UnitTypes.FirstAsync(t => t.IsActive);
            var property = new Property(UniqueName("Test property"), TestAddress());
            var unit = property.AddUnit("101", 1, 1200m, unitType);

            db.Properties.Add(property);
            await db.SaveChangesAsync();
            return unit.Id;
        });
    }

    public static async Task<T> QueryDbAsync<T>(
        this PropertyManagementWebFactory factory, Func<PropertyManagementDbContext, Task<T>> query)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<PropertyManagementDbContext>());
    }

    public static Task<string> UserIdAsync(this PropertyManagementWebFactory factory, string email)
    {
        return factory.QueryDbAsync(db => db.Users.Where(u => u.Email == email).Select(u => u.Id).SingleAsync());
    }

    public static DateOnly BusinessToday(this PropertyManagementWebFactory factory)
    {
        return factory.Services.GetRequiredService<TimeProvider>().Today();
    }

    public static FeatureOverride EnableSaveInvalidSections(this PropertyManagementWebFactory factory)
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        return new FeatureOverride(configuration, "Features:SaveInvalidSections", "true");
    }

    public static string UniqueName(string prefix)
    {
        return $"{prefix} {Guid.NewGuid():N}"[..(prefix.Length + 9)];
    }

    private static Address TestAddress()
    {
        return new Address("1 Main St", "Albany", "NY", "12207");
    }

    private static ApplicantDetails ApplicantDetails()
    {
        return new ApplicantDetails
        {
            FirstName = "Jane",
            LastName = "Doe",
            Phone = "(518) 555-0100",
            Email = "jane@example.com",
            Street = "1 Main St",
            City = "Albany",
            State = "NY",
            PostalCode = "12207"
        };
    }

    public static ResidenceDetails Residence()
    {
        return new ResidenceDetails
        {
            Address = TestAddress(),
            LandlordName = "Bob Landlord",
            LandlordPhone = "(518) 555-0199",
            MoveInDate = new DateOnly(2022, 1, 1),
            MoveOutDate = new DateOnly(2025, 12, 31)
        };
    }
}
