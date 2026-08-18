namespace Askii.Common.Authorization;

public interface IPermissionRegistry
{
    bool RuoloHa(string? ruolo, string permesso);

    IReadOnlyCollection<string> PermessiDi(string? ruolo);
}

/// <summary>
/// Mappa ruolo → permessi, definita in un posto solo.
///
/// I permessi NON finiscono nel token: il token porta il ruolo, e la mappa viene
/// consultata a ogni richiesta. Così cambiare l'assegnazione ha effetto subito,
/// senza aspettare che scadano i token già emessi, e i token non crescono a ogni
/// permesso aggiunto.
/// </summary>
public class PermissionRegistry : IPermissionRegistry
{
    private readonly Dictionary<string, HashSet<string>> _perRuolo;

    public PermissionRegistry(IDictionary<string, IEnumerable<string>>? mappa = null)
    {
        var sorgente = mappa ?? MappaPredefinita();

        // Un permesso scritto male non darebbe errore: negherebbe l'accesso in
        // silenzio, e il difetto salterebbe fuori solo quando qualcuno prova a
        // usare la funzione. Meglio non partire.
        var ignoti = sorgente.SelectMany(v => v.Value)
            .Where(p => !Permissions.Tutti.Contains(p))
            .Distinct()
            .ToList();

        if (ignoti.Count > 0)
        {
            throw new InvalidOperationException(
                $"Permessi non dichiarati in Permissions: {string.Join(", ", ignoti)}");
        }

        _perRuolo = sorgente.ToDictionary(
            v => v.Key,
            v => new HashSet<string>(v.Value, StringComparer.Ordinal),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// Assegnazione predefinita. Client non ha permessi amministrativi: sul
    /// proprio account agisce comunque, perché quei controlli sono sull'identità
    /// e non su un permesso.
    /// </summary>
    private static Dictionary<string, IEnumerable<string>> MappaPredefinita() => new()
    {
        [Roles.Admin] = Permissions.Tutti,

        [Roles.Operator] =
        [
            Permissions.Users.Read,
            Permissions.Settings.Read,
        ],

        [Roles.Client] = [],
    };

    public bool RuoloHa(string? ruolo, string permesso)
        => ruolo is not null
           && _perRuolo.TryGetValue(ruolo, out var permessi)
           && permessi.Contains(permesso);

    public IReadOnlyCollection<string> PermessiDi(string? ruolo)
        => ruolo is not null && _perRuolo.TryGetValue(ruolo, out var permessi)
            ? permessi
            : [];
}
