import { useCallback, useEffect, useState } from 'react'
import { ApiError } from '@/api/client'

interface Stato<T> {
  dati: T | null
  inCorso: boolean
  errore: string | null
}

/**
 * Lettura che si ri-esegue quando cambiano le dipendenze.
 *
 * Diverso da useAzione, che serve alle scritture avviate dall'utente: qui il
 * caricamento parte da solo. Le risposte di richieste superate vengono scartate,
 * altrimenti digitando in fretta nella ricerca l'ultima risposta ad arrivare
 * potrebbe non essere quella dell'ultima richiesta partita.
 */
export function useRisorsa<T>(carica: () => Promise<T>, deps: unknown[]) {
  const [stato, setStato] = useState<Stato<T>>({ dati: null, inCorso: true, errore: null })
  const [contatore, setContatore] = useState(0)

  const ricarica = useCallback(() => setContatore((n) => n + 1), [])

  useEffect(() => {
    let superata = false
    setStato((prec) => ({ ...prec, inCorso: true, errore: null }))

    carica()
      .then((dati) => {
        if (!superata) setStato({ dati, inCorso: false, errore: null })
      })
      .catch((e: unknown) => {
        if (superata) return
        const errore =
          e instanceof ApiError || e instanceof Error
            ? e.message
            : 'Errore imprevisto durante la lettura'
        setStato({ dati: null, inCorso: false, errore })
      })

    return () => {
      superata = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, contatore])

  return { ...stato, ricarica }
}
