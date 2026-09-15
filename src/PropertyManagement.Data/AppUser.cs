using Microsoft.AspNetCore.Identity;

namespace PropertyManagement.Data;

public class AppUser : IdentityUser
{
    public const int NameMaxLength = 100;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
