namespace Askii.Common.Paging;

/// <summary>
/// Parametri di paginazione e ordinamento già normalizzati.
///
/// I DTO di richiesta degli endpoint restano separati, perché i filtri cambiano
/// da risorsa a risorsa: quello che si condivide è questa normalizzazione, non
/// la forma della request.
/// </summary>
public record PageRequest
{
    public const int DimensionePredefinita = 25;
    public const int DimensioneMassima = 100;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = DimensionePredefinita;

    /// <summary>Chiave di ordinamento, già in minuscolo. Null = usa il default.</summary>
    public string? Sort { get; init; }

    public bool Desc { get; init; }

    public int Skip => (Page - 1) * PageSize;

    /// <summary>
    /// Corregge i valori fuori range invece di rifiutarli: una pagina 0 o una
    /// dimensione assurda sono errori del client che non vale la pena
    /// trasformare in un 400. Il tetto massimo però è invalicabile, altrimenti
    /// una richiesta con pageSize enorme diventa un modo per saturare il server.
    /// </summary>
    public static PageRequest From(int? page, int? pageSize, string? sort, string? dir) => new()
    {
        Page = page is null or < 1 ? 1 : page.Value,
        PageSize = Math.Clamp(pageSize ?? DimensionePredefinita, 1, DimensioneMassima),
        Sort = string.IsNullOrWhiteSpace(sort) ? null : sort.Trim().ToLowerInvariant(),
        Desc = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase),
    };
}
