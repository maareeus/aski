import { useEffect, useState } from 'react'

/** Ritarda la propagazione di un valore, per non chiamare l'API a ogni tasto. */
export function useDebounce<T>(valore: T, ritardoMs = 300): T {
  const [ritardato, setRitardato] = useState(valore)

  useEffect(() => {
    const timer = window.setTimeout(() => setRitardato(valore), ritardoMs)
    return () => window.clearTimeout(timer)
  }, [valore, ritardoMs])

  return ritardato
}
