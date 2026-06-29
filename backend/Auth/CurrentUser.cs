using System.Security.Claims;
using Aski.Tickets.Api.Domain;

namespace Aski.Tickets.Api.Auth;

/// <summary>Estrae l'identità applicativa dai claim del JWT.</summary>
public static class CurrentUser
{
    public static string Id(this ClaimsPrincipal u) => u.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public static int? CompanyId(this ClaimsPrincipal u) =>
        int.TryParse(u.FindFirstValue(AppClaims.CompanyId), out var id) ? id : null;

    public static bool IsAdmin(this ClaimsPrincipal u) => u.IsInRole(Roles.Admin);
    public static bool IsAgent(this ClaimsPrincipal u) => u.IsInRole(Roles.Agent);
    public static bool IsPm(this ClaimsPrincipal u) => u.IsInRole(Roles.PM);
    public static bool IsClient(this ClaimsPrincipal u) => u.IsInRole(Roles.Client);

    /// <summary>True se è staff (Admin, PM o Agent).</summary>
    public static bool IsStaff(this ClaimsPrincipal u) => u.IsAdmin() || u.IsPm() || u.IsAgent();
}
