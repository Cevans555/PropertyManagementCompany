using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.ValueObjects;
using PropertyManagement.Data;
using PropertyManagement.Data.Auditing;
using PropertyManagement.Data.Queries;
using PropertyManagement.Data.Services;

namespace PropertyManagement.IntegrationTests.Infrastructure;


public sealed class TestDatabase : IAsyncLifetime
{
    private readonly string _databaseName = $"PropertyManagementTests_{Guid.NewGuid():N}";
    private readonly TestCurrentUser _currentUser = new();

    public TimeProvider Clock { get; } = new BusinessTimeProvider(TimeZoneInfo.FindSystemTimeZoneById("America/New_York"));

    public DateOnly Today => Clock.Today();

    private string ConnectionString =>
        $@"Server=(localdb)\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;MultipleActiveResultSets=true";

    public PropertyManagementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PropertyManagementDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsAssembly("PropertyManagement.Data"))
            .AddInterceptors(new AuditSaveChangesInterceptor(_currentUser, Clock))
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
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
}
