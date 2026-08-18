using Askii.Common.Helpers;
using Askii.Database;
using Askii.Features.Auth;
using Microsoft.EntityFrameworkCore;

namespace Askii.Features.Users.GetUser;

public static class GetUserEndpoint
{
    public static async Task<IResult> Impl(
        Guid id,
        AppDbContext db,
        CancellationToken ct)
    {
        // Proiezione dentro la query, come nella lista: PasswordHash non lascia
        // il database.
        var utente = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDetail(
                u.Id,
                u.Email,
                u.Name,
                u.LastName,
                u.Role,
                u.IsActive,
                u.IsSuperAdmin,
                u.LastLoginUtc,
                u.CreatedAtUtc,
                u.UpdatedAtUtc,
                u.TFA_Availables))
            .SingleOrDefaultAsync(ct);

        // Qui il 404 è la risposta corretta: la risorsa non esiste, non è la
        // richiesta a essere malformata.
        return utente is null
            ? ResultsHelper.NotFound($"Nessun utente con identificativo {id}")
            : Results.Ok(utente);
    }
}

public record UserDetail(
    Guid Id,
    string Email,
    string Name,
    string LastName,
    string Role,
    bool IsActive,
    bool IsSuperAdmin,
    DateTime? LastLoginUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    List<TFA_Available> TFA_Availables)
{
    public string FullName => $"{Name} {LastName}".Trim();
}
