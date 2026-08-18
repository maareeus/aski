using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Askii.Common.Paging;

public static class PagedQueryableExtensions
{
    /// <summary>
    /// Conta, pagina e proietta in un colpo solo.
    ///
    /// Accetta un <see cref="IOrderedQueryable{T}"/> e non un IQueryable: Skip e
    /// Take su una query non ordinata danno risultati non deterministici, e con
    /// questa firma il compilatore obbliga a ordinare prima.
    ///
    /// La proiezione è un'Expression e viene applicata dentro la query, quindi
    /// il database restituisce solo le colonne che servono: le altre — per
    /// l'utente, PasswordHash — non lasciano nemmeno il db.
    /// </summary>
    public static async Task<PagedResult<TOut>> ToPagedResultAsync<TIn, TOut>(
        this IOrderedQueryable<TIn> query,
        PageRequest paging,
        Expression<Func<TIn, TOut>> proiezione,
        CancellationToken ct = default)
    {
        // Due round trip: uno per il totale, uno per la pagina. Il conteggio su
        // un filtro con LIKE '%x%' costa quanto la pagina stessa; se diventasse
        // un problema, l'alternativa è chiedere PageSize + 1 righe e restituire
        // solo HasNext, rinunciando a TotalCount.
        var totale = await query.CountAsync(ct);

        var items = await query
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(proiezione)
            .ToListAsync(ct);

        return new PagedResult<TOut>(items, paging.Page, paging.PageSize, totale);
    }
}
