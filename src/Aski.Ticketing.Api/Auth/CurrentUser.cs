using System.Security.Claims;
using Aski.Ticketing.Api.Domain;

namespace Aski.Ticketing.Api.Auth;

/// <summary>Custom claim types usati nei JWT dell'istanza.</summary>
public static class AskiClaims
{
    public const string CompanyId = "companyId";
}

/// <summary>Estrae l'identità applicativa dai claim del JWT corrente.</summary>
public static class CurrentUser
{
    public static int Id(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public static TicketRole Role(this ClaimsPrincipal user) =>
        Enum.TryParse<TicketRole>(user.FindFirstValue(ClaimTypes.Role), out var r) ? r : TicketRole.Client;

    /// <summary>CompanyId del Client (null per Admin/Dev non vincolati).</summary>
    public static int? CompanyId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(AskiClaims.CompanyId), out var id) ? id : null;
}
