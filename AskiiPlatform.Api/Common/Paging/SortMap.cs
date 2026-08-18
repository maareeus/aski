namespace Askii.Common.Paging;

/// <summary>
/// Elenco chiuso degli ordinamenti ammessi per un tipo.
///
/// Serve a due cose: impedire che una chiave arrivata dal client finisca in una
/// espressione di ordinamento, e garantire che ogni ordinamento porti con sé un
/// tiebreaker. Senza tiebreaker, ordinando su una colonna con pochi valori
/// distinti l'ordine fra pagine non è deterministico e si vedono righe
/// duplicate o mancanti passando da una pagina all'altra.
///
/// Si usano delegati e non Expression&lt;Func&lt;T, object&gt;&gt; perché quest'ultima
/// introduce una conversione a object che EF non sempre traduce.
/// </summary>
public sealed class SortMap<T>
{
    private readonly Dictionary<string, Func<IQueryable<T>, bool, IOrderedQueryable<T>>> _ordinamenti;
    private readonly string _predefinito;

    public SortMap(
        string predefinito,
        Dictionary<string, Func<IQueryable<T>, bool, IOrderedQueryable<T>>> ordinamenti)
    {
        if (!ordinamenti.ContainsKey(predefinito))
        {
            throw new ArgumentException(
                $"L'ordinamento predefinito '{predefinito}' non è fra quelli dichiarati.",
                nameof(predefinito));
        }

        _predefinito = predefinito;
        _ordinamenti = new Dictionary<string, Func<IQueryable<T>, bool, IOrderedQueryable<T>>>(
            ordinamenti, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Chiavi accettate, utili da esporre nella documentazione dell'API.</summary>
    public IReadOnlyCollection<string> ChiaviAmmesse => _ordinamenti.Keys;

    /// <summary>
    /// Una chiave sconosciuta ricade sul default senza errore: l'ordinamento è
    /// una preferenza di presentazione, non un dato da validare.
    /// </summary>
    public IOrderedQueryable<T> Apply(IQueryable<T> query, PageRequest paging)
    {
        var chiave = paging.Sort is not null && _ordinamenti.ContainsKey(paging.Sort)
            ? paging.Sort
            : _predefinito;

        return _ordinamenti[chiave](query, paging.Desc);
    }
}
