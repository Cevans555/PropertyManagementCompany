using System.Security.Claims;

namespace PropertyManagement.Web.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static string Id(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}
