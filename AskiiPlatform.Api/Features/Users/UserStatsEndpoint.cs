using Askii.Common;
using Askii.Database;
using Microsoft.EntityFrameworkCore;

namespace Askii.Features.Users.Stats;

/// <summary>
/// Conteggi per il riepilogo.
///
/// Non si ricavano lato client dalla lista, che è paginata: servirebbe scaricare
/// tutte le pagine, e il totale cambierebbe fra la prima e l'ultima richiesta.
/// </summary>
public static class UserStatsEndpoint
{
    public static async Task<IResult> Impl(AppDbContext db, CancellationToken ct)
    {
        // Una sola passata sul database: i conteggi condizionali stanno tutti
        // nella stessa query di aggregazione, invece di una COUNT per riga.
        var conteggi = await db.Users
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Totale = g.Count(),
                Attivi = g.Count(u => u.IsActive),
                DaAttivare = g.Count(u => !u.IsActive),
                ConTfa = g.Count(u => u.TFA_Availables.Count > 0),
                Admin = g.Count(u => u.Role == Roles.Admin),
                Operator = g.Count(u => u.Role == Roles.Operator),
                Client = g.Count(u => u.Role == Roles.Client),
            })
            .SingleOrDefaultAsync(ct);

        // GroupBy su tabella vuota non restituisce righe: i conteggi sono zero.
        if (conteggi is null)
        {
            return Results.Ok(new UserStatsResult(0, 0, 0, 0, new Dictionary<string, int>(), null));
        }

        var ultimoAccesso = await db.Users
            .AsNoTracking()
            .Where(u => u.LastLoginUtc != null)
            .MaxAsync(u => u.LastLoginUtc, ct);

        return Results.Ok(new UserStatsResult(
            Total: conteggi.Totale,
            Active: conteggi.Attivi,
            PendingActivation: conteggi.DaAttivare,
            WithTfa: conteggi.ConTfa,
            ByRole: new Dictionary<string, int>
            {
                [Roles.Admin] = conteggi.Admin,
                [Roles.Operator] = conteggi.Operator,
                [Roles.Client] = conteggi.Client,
            },
            LastLoginUtc: ultimoAccesso));
    }
}

public record UserStatsResult(
    int Total,
    int Active,
    int PendingActivation,
    int WithTfa,
    IReadOnlyDictionary<string, int> ByRole,
    DateTime? LastLoginUtc);
