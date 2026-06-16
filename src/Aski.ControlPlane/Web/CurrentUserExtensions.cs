using System.Security.Claims;

namespace Aski.ControlPlane.Web;

/// <summary>Accesso ai claim dell'utente loggato nel Control Plane.</summary>
public static class CurrentUserExtensions
{
    /// <summary>Id del tenant del cliente loggato (null per SuperAdmin).</summary>
    public static int? TenantId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue("tenantId"), out var id) ? id : null;

    public static int UserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public static bool IsSuperAdmin(this ClaimsPrincipal user) => user.IsInRole("SuperAdmin");
}
