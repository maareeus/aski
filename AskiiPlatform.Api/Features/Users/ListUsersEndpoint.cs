using Askii.Common.Paging;
using Askii.Database;
using Askii.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Askii.Features.Users.ListUsers;

public static class ListUsersEndpoint
{
    /// <summary>
    /// Ogni voce include il tiebreaker su Id: senza di esso, ordinando per Role
    /// o IsActive (pochi valori distinti) l'ordine fra pagine non è garantito.
    /// </summary>
    private static readonly SortMap<User> Ordinamenti = new(
        predefinito: "email",
        ordinamenti: new()
        {
            ["email"] = (q, desc) => desc
                ? q.OrderByDescending(u => u.Email).ThenBy(u => u.Id)
                : q.OrderBy(u => u.Email).ThenBy(u => u.Id),

            ["lastname"] = (q, desc) => desc
                ? q.OrderByDescending(u => u.LastName).ThenBy(u => u.Id)
                : q.OrderBy(u => u.LastName).ThenBy(u => u.Id),

            ["role"] = (q, desc) => desc
                ? q.OrderByDescending(u => u.Role).ThenBy(u => u.Id)
                : q.OrderBy(u => u.Role).ThenBy(u => u.Id),

            ["status"] = (q, desc) => desc
                ? q.OrderByDescending(u => u.IsActive).ThenBy(u => u.Id)
                : q.OrderBy(u => u.IsActive).ThenBy(u => u.Id),

            ["lastlogin"] = (q, desc) => desc
                ? q.OrderByDescending(u => u.LastLoginUtc).ThenBy(u => u.Id)
                : q.OrderBy(u => u.LastLoginUtc).ThenBy(u => u.Id),

            ["created"] = (q, desc) => desc
                ? q.OrderByDescending(u => u.CreatedAtUtc).ThenBy(u => u.Id)
                : q.OrderBy(u => u.CreatedAtUtc).ThenBy(u => u.Id),
        });

    private const string LikeEscape = "\\";

    private static string EscapeLike(string testo) => testo
        .Replace(LikeEscape, LikeEscape + LikeEscape)
        .Replace("%", LikeEscape + "%")
        .Replace("_", LikeEscape + "_");

    public static async Task<IResult> Impl(
        [AsParameters] ListUsersRequest req,
        AppDbContext db,
        CancellationToken ct)
    {
        var paging = PageRequest.From(req.Page, req.PageSize, req.Sort, req.Dir);

        // AsNoTracking: è una lettura, non serve il change tracker.
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            // string.Contains su SQLite viene tradotto in instr(), che ignora la
            // collation della colonna ed è quindi case-sensitive: cercare
            // "ROSSI" non troverebbe "Rossi". LIKE invece è case-insensitive
            // sull'ASCII, che è il comportamento atteso da una casella di
            // ricerca. I metacaratteri nell'input vanno neutralizzati, altrimenti
            // un "%" digitato dall'utente diventa un jolly.
            var pattern = $"%{EscapeLike(req.Search.Trim())}%";

            query = query.Where(u =>
                EF.Functions.Like(u.Email, pattern, LikeEscape) ||
                EF.Functions.Like(u.Name, pattern, LikeEscape) ||
                EF.Functions.Like(u.LastName, pattern, LikeEscape));
        }

        if (!string.IsNullOrWhiteSpace(req.Role))
        {
            query = query.Where(u => u.Role == req.Role);
        }

        if (req.IsActive is not null)
        {
            query = query.Where(u => u.IsActive == req.IsActive);
        }

        var risultato = await Ordinamenti
            .Apply(query, paging)
            .ToPagedResultAsync(
                paging,
                u => new UserListItem(
                    u.Id,
                    u.Email,
                    u.Name,
                    u.LastName,
                    u.Role,
                    u.IsActive,
                    u.IsSuperAdmin,
                    u.LastLoginUtc,
                    u.CreatedAtUtc),
                ct);

        return Results.Ok(risultato);
    }
}

/// <summary>
/// Filtri, paginazione e ordinamento. Va bindata con [AsParameters], altrimenti
/// una minimal API la tratterebbe come body JSON e su una GET non funzionerebbe.
///
/// I parametri non applicati vanno omessi dalla query string, non inviati vuoti:
/// `?isActive=` fa fallire il binding di bool? con un 400.
/// </summary>
public record ListUsersRequest(
    string? Search,
    string? Role,
    bool? IsActive,
    int? Page,
    int? PageSize,
    string? Sort,
    string? Dir);

/// <summary>
/// Solo le colonne che la tabella disegna. Non si restituisce User perché ha
/// PasswordHash con getter pubblico, che finirebbe serializzato ai client.
/// </summary>
public record UserListItem(
    Guid Id,
    string Email,
    string Name,
    string LastName,
    string Role,
    bool IsActive,
    bool IsSuperAdmin,
    DateTime? LastLoginUtc,
    DateTime CreatedAtUtc)
{
    public string FullName => $"{Name} {LastName}".Trim();
}
