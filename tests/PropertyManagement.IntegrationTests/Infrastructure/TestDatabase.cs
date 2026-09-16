using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.ValueObjects;
using PropertyManagement.Data;
using PropertyManagement.Data.Auditing;
using PropertyManagement.Data.Queries;
using PropertyManagement.Data.Services;

namespace PropertyManagement.IntegrationTests.Infrastructure;

/// <summary>
/// A real LocalDB database for one test class: created from the migrations before the tests run and
/// dropped afterwards. The database name is unique, so test classes never collide.
/// </summary>
public sealed class TestDatabase : IAsyncLifetime
{
    private readonly string _databaseName = $"PropertyManagementTests_{Guid.NewGuid():N}";
    private readonly TestCurrentUser _currentUser = new();
    private readonly IServiceProvider _appServices = BuildAppServices();

    public TimeProvider Clock { get; } = new BusinessTimeProvider(TimeZoneInfo.FindSystemTimeZoneById("America/New_York"));

    public DateOnly Today => Clock.Today();

    private string ConnectionString =>
        $@"Server=(localdb)\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;MultipleActiveResultSets=true";

    public PropertyManagementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PropertyManagementDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsAssembly("PropertyManagement.Data"))
            .AddInterceptors(new AuditSaveChangesInterceptor(_currentUser, Clock))
            .UseApplicationServiceProvider(_appServices)
            .Options;

        return new PropertyManagementDbContext(options);
    }

    public RentalApplicationService ApplicantService(PropertyManagementDbContext db)
    {
        return new RentalApplicationService(db, new ApplicationUpdater(db, Clock), new LeaseQueries(db), Clock);
    }

    public ApplicationReviewService ReviewService(PropertyManagementDbContext db)
    {
        return new ApplicationReviewService(new ApplicationUpdater(db, Clock), new LeaseQueries(db), Clock);
    }

    /// <summary>Creates a user row, so the applicant and manager foreign keys point at something real.</summary>
    public async Task<string> CreateUserAsync(PropertyManagementDbContext db, string role)
    {
        var email = $"{role}-{Guid.NewGuid():N}@test.local";
        var user = new AppUser
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            FirstName = "Test",
            LastName = role
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    /// <summary>Creates a property with one unit and returns the unit's id.</summary>
    public async Task<int> CreateUnitAsync(PropertyManagementDbContext db)
    {
        var unitType = new UnitType($"Standard-{Guid.NewGuid():N}");
        var property = new Property("Maple Commons", new Address("1 Main St", "Albany", "NY", "12207"));
        var unit = property.AddUnit("101", 2, 1500m, unitType);

        db.UnitTypes.Add(unitType);
        db.Properties.Add(property);
        await db.SaveChangesAsync();
        return unit.Id;
    }

    public async Task InitializeAsync()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }

    /// <summary>
    /// Identity reads its store options from the application's service provider while building the model.
    /// The app gets one from AddDbContext; a context built by hand needs the same options, or Identity's
    /// composite key columns come out as nvarchar(450) instead of the migration's nvarchar(128).
    /// </summary>
    private static IServiceProvider BuildAppServices()
    {
        var services = new ServiceCollection();
        services.Configure<IdentityOptions>(options => options.Stores.MaxLengthForKeys = 128);
        return services.BuildServiceProvider();
    }
}
