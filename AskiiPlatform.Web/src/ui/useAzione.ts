import { useCallback, useState } from 'react'
import { ApiError } from '@/api/client'

interface Stato<T> {
  inCorso: boolean
  errore: string | null
  esito: T | null
}

/**
 * Incapsula il ciclo invio/attesa/esito di una chiamata all'API, così le
 * pagine non ripetono ogni volta try/catch e flag di caricamento.
 */
export function useAzione<TArgs extends unknown[], TResult>(
  azione: (...args: TArgs) => Promise<TResult>,
) {
  const [stato, setStato] = useState<Stato<TResult>>({
    inCorso: false,
    errore: null,
    esito: null,
  })

  const esegui = useCallback(
    async (...args: TArgs): Promise<TResult | null> => {
      setStato({ inCorso: true, errore: null, esito: null })
      try {
        const esito = await azione(...args)
        setStato({ inCorso: false, errore: null, esito })
        return esito
      } catch (e) {
        const errore =
          e instanceof ApiError
            ? e.message
            : e instanceof Error
              ? e.message
              : 'Errore imprevisto durante la chiamata al servizio'
        setStato({ inCorso: false, errore, esito: null })
        return null
      }
    },
    [azione],
  )

  const reset = useCallback(() => setStato({ inCorso: false, errore: null, esito: null }), [])

  return { ...stato, esegui, reset }
}
