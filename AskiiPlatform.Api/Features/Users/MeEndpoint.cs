using Askii.Common;
using System.Security.Claims;
using Askii.Common.Extensions;
using Askii.Common.Helpers;
using Askii.Database;
using Askii.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Askii.Features.Users.Me;

/// <summary>
/// Profilo dell'utente collegato, letto dal database.
///
/// Serve perché il client altrimenti si fida della risposta di login conservata
/// in locale, che invecchia: un cambio di ruolo o di anagrafica fatto da un
/// amministratore non si vedrebbe fino al prossimo accesso.
/// </summary>
public static class MeEndpoint
{
    public static async Task<IResult> Impl(
        AppDbContext db, ClaimsPrincipal loggedUser, CancellationToken ct)
    {
        var id = loggedUser.CurrentUserId();

        var profilo = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new MeResult(
                u.Id,
                u.Email,
                u.Name,
                u.LastName,
                u.Role,
                u.IsActive,
                u.IsSuperAdmin,
                u.LastLoginUtc,
                u.CreatedAtUtc,
                u.TFA_Availables))
            .SingleOrDefaultAsync(ct);

        // Il token è valido ma l'utente non c'è più: può succedere solo in una
        // finestra strettissima, dato che OnTokenValidated lo verifica.
        return profilo is null
            ? ResultsHelper.NotFound("Utente non trovato")
            : Results.Ok(profilo);
    }
}

public record MeResult(
    Guid Id,
    string Email,
    string Name,
    string LastName,
    string Role,
    bool IsActive,
    bool IsSuperAdmin,
    DateTime? LastLoginUtc,
    DateTime CreatedAtUtc,
    List<TFA_Available> TfaMethods)
{
    public string FullName => $"{Name} {LastName}".Trim();
    public bool TfaEnabled => TfaMethods.Count > 0;
}
