import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import { ROLE_LIST, USER_SORT } from '@/api/types'
import type { Role, UserListQuery, UserSort } from '@/api/types'

const PAGE_SIZE_AMMESSE = [10, 25, 50, 100] as const

/**
 * Stato di filtri, ordinamento e pagina tenuto nella query string invece che in
 * useState: così ricaricare la pagina, tornare indietro col browser e mandare un
 * link a un collega funzionano tutti senza codice aggiuntivo.
 *
 * I valori che arrivano dalla URL sono input non fidato, quindi ognuno viene
 * validato contro l'elenco di quelli ammessi.
 */
export function useUserListQuery() {
  const [params, setParams] = useSearchParams()

  const query = useMemo<UserListQuery>(() => {
    const sort = params.get('sort')
    const dir = params.get('dir')
    const role = params.get('role')
    const stato = params.get('isActive')
    const page = Number(params.get('page'))
    const pageSize = Number(params.get('pageSize'))

    return {
      search: params.get('search') ?? '',
      role: role && (ROLE_LIST as readonly string[]).includes(role) ? (role as Role) : '',
      isActive: stato === 'true' ? true : stato === 'false' ? false : '',
      page: Number.isFinite(page) && page >= 1 ? page : 1,
      pageSize: (PAGE_SIZE_AMMESSE as readonly number[]).includes(pageSize) ? pageSize : 25,
      sort: sort && (USER_SORT as readonly string[]).includes(sort) ? (sort as UserSort) : 'email',
      dir: dir === 'desc' ? 'desc' : 'asc',
    }
  }, [params])

  /**
   * Un cambio di filtro riporta a pagina 1: restare a pagina 7 dopo aver
   * ristretto il risultato a due righe mostrerebbe una tabella vuota.
   */
  const aggiorna = useCallback(
    (modifiche: Partial<UserListQuery>) => {
      const tornaAPagina1 = Object.keys(modifiche).some((k) => k !== 'page')

      setParams(
        (prec) => {
          const next = new URLSearchParams(prec)

          for (const [chiave, valore] of Object.entries(modifiche)) {
            if (valore === '' || valore === undefined || valore === null) next.delete(chiave)
            else next.set(chiave, String(valore))
          }
          if (tornaAPagina1 && modifiche.page === undefined) next.delete('page')

          return next
        },
        { replace: true },
      )
    },
    [setParams],
  )

  /** Inverte la direzione se si riclicca la colonna già ordinata. */
  const ordinaPer = useCallback(
    (colonna: UserSort) => {
      const stessaColonna = query.sort === colonna
      aggiorna({ sort: colonna, dir: stessaColonna && query.dir === 'asc' ? 'desc' : 'asc' })
    },
    [aggiorna, query.sort, query.dir],
  )

  const azzera = useCallback(() => setParams(new URLSearchParams(), { replace: true }), [setParams])

  const filtriAttivi =
    query.search !== '' || query.role !== '' || query.isActive !== ''

  return { query, aggiorna, ordinaPer, azzera, filtriAttivi, PAGE_SIZE_AMMESSE }
}
