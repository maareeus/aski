using System.Security.Claims;

namespace Askii.Common.Extensions;

public static class CurrentUserClaimsPrincipalExtensions
{
    public static Guid CurrentUserId(this ClaimsPrincipal loggedUser)
    {
        var cuidstr = loggedUser.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(cuidstr!);
    }

    public static string CurrentUserEmail(this ClaimsPrincipal loggedUser)
    {
        return loggedUser.FindFirstValue(ClaimTypes.Email)!;
    }

    public static string CurrentUserRole(this ClaimsPrincipal loggedUser)
    {
        return loggedUser.FindFirstValue(ClaimTypes.Role)!;
    }
}