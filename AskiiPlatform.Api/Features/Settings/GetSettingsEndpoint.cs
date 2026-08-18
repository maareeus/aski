using Askii.Database;
using Askii.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Askii.Features.Settings.GetSettings;

public static class GetSettingsEndpoint
{
    /// <summary>
    /// Opzioni il cui valore non viene mai restituito. Il client sa soltanto se
    /// una password è configurata, e può sovrascriverla: leggerla non serve a
    /// nessuna schermata e un GET la esporrebbe a log, cache e cronologia.
    /// </summary>
    private static readonly HashSet<string> Segrete = new(StringComparer.OrdinalIgnoreCase)
    {
        Option.Email.SMTP_PASS,
    };

    public static async Task<IResult> Impl(AppDbContext db, CancellationToken ct)
    {
        // Lettura diretta dal database e non dalla cache del singleton Options:
        // la cache viene popolata all'avvio e aggiornata solo dalle scritture
        // passate da quell'istanza, quindi non è garantita fresca.
        var opzioni = await db.Options
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .Select(o => new { o.Name, o.Value, o.LastUpdateUtc })
            .ToListAsync(ct);

        var items = opzioni
            .Select(o =>
            {
                var segreta = Segrete.Contains(o.Name);
                return new SettingItem(
                    Name: o.Name,
                    Value: segreta ? null : o.Value,
                    IsSecret: segreta,
                    HasValue: !string.IsNullOrEmpty(o.Value),
                    LastUpdateUtc: o.LastUpdateUtc);
            })
            .ToList();

        return Results.Ok(new SettingsResult(items));
    }
}

public record SettingsResult(IReadOnlyList<SettingItem> Items);

public record SettingItem(
    string Name,
    /// <summary>Null per le opzioni segrete: si usa HasValue per sapere se è impostata.</summary>
    string? Value,
    bool IsSecret,
    bool HasValue,
    DateTime LastUpdateUtc);
