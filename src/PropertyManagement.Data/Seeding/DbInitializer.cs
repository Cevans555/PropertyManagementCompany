using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyManagement.Core.Security;

namespace PropertyManagement.Data.Seeding;

public static class DbInitializer
{
    public const string DemoPassword = "Password123!";

    private const int ManagerCount = 2;
    private const int ApplicantCount = 8;

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<PropertyManagementDbContext>();
        var userManager = provider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync(cancellationToken);

        var faker = new Faker { Random = new Randomizer(20260913) };

        await IdentitySeeder.SeedRolesAsync(roleManager);
        var unitTypes = await PropertySeeder.SeedUnitTypesAsync(db, cancellationToken);
        var managers = await IdentitySeeder.SeedUsersAsync(userManager, faker, "manager", ManagerCount, Roles.PropertyManager);
        var applicants = await IdentitySeeder.SeedUsersAsync(userManager, faker, "applicant", ApplicantCount, Roles.Applicant);

        if (await db.Properties.AnyAsync(cancellationToken))
            return;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var audit = new SeedAuditStamps();

        var properties = PropertySeeder.CreateProperties(faker, unitTypes, managers, now, audit);
        db.Properties.AddRange(properties);
        audit.ApplyTo(db);
        await db.SaveChangesAsync(cancellationToken);

        await ApplicationSeeder.SeedAsync(db, faker, properties, managers, applicants, now, audit, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
