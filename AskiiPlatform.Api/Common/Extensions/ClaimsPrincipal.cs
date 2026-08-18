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

    /// <summary>
    /// Versione che non finge: le altre sopprimono il null con `!` e vanno usate
    /// solo dove la pipeline garantisce il claim. Qui l'assenza è un caso
    /// previsto, perché il controllo dei permessi gira anche su principal
    /// incompleti e deve semplicemente negare.
    /// </summary>
    public static string? CurrentUserRoleOrNull(this ClaimsPrincipal loggedUser)
        => loggedUser.FindFirstValue(ClaimTypes.Role);

    /// <summary>Null invece di eccezione quando il claim manca o non è un Guid.</summary>
    public static Guid? CurrentUserIdOrNull(this ClaimsPrincipal loggedUser)
        => Guid.TryParse(loggedUser.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;
}