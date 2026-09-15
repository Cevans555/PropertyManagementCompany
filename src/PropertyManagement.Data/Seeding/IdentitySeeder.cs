using Bogus;
using Microsoft.AspNetCore.Identity;
using PropertyManagement.Core.Security;

namespace PropertyManagement.Data.Seeding;

internal static class IdentitySeeder
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(role)), $"role {role}");
        }
    }

    public static async Task<List<AppUser>> SeedUsersAsync(
        UserManager<AppUser> userManager, Faker faker, string emailPrefix, int count, string role)
    {
        var users = new List<AppUser>();

        for (var i = 1; i <= count; i++)
        {
            var email = $"{emailPrefix}{i}@demo.com";

            var firstName = faker.Name.FirstName();
            var lastName = faker.Name.LastName();

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = firstName,
                    LastName = lastName
                };

                EnsureSucceeded(await userManager.CreateAsync(user, DbInitializer.DemoPassword), email);
            }

            if (!await userManager.IsInRoleAsync(user, role))
                EnsureSucceeded(await userManager.AddToRoleAsync(user, role), email);

            users.Add(user);
        }

        return users;
    }

    private static void EnsureSucceeded(IdentityResult result, string subject)
    {
        if (result.Succeeded)
            return;

        var errors = string.Join("; ", result.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Seeding {subject} failed: {errors}");
    }
}
