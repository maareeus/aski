namespace Askii.Common.Paging;

/// <summary>
/// Busta di risposta per gli elenchi paginati. Le proprietà calcolate vengono
/// serializzate insieme alle altre, così il client non deve rifare l'aritmetica
/// della paginazione.
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}
