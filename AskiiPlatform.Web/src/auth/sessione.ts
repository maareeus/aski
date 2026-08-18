import type { Role } from '@/api/types'
import { isExpired } from './jwt'

/**
 * La sessione vive in localStorage, che è la sorgente di verità. AuthProvider la
 * rispecchia nello stato React per il rendering, ma il client HTTP la legge da
 * qui direttamente.
 *
 * È una separazione necessaria, non stilistica: in React gli effect dei figli
 * girano prima di quelli del padre, quindi una pagina che carica dati nel proprio
 * effect partirebbe prima che AuthProvider abbia potuto passare il token al
 * client, e la prima richiesta uscirebbe senza Authorization.
 */

export const CHIAVE_SESSIONE = 'askii.session'

export interface Session {
  token: string
  userId: string
  email: string
  fullName: string
  role: Role
}

export function leggiSessione(): Session | null {
  const raw = localStorage.getItem(CHIAVE_SESSIONE)
  if (!raw) return null

  try {
    const sessione = JSON.parse(raw) as Session
    if (!sessione.token || isExpired(sessione.token)) {
      localStorage.removeItem(CHIAVE_SESSIONE)
      return null
    }
    return sessione
  } catch {
    localStorage.removeItem(CHIAVE_SESSIONE)
    return null
  }
}

export function salvaSessione(sessione: Session): void {
  localStorage.setItem(CHIAVE_SESSIONE, JSON.stringify(sessione))
}

export function rimuoviSessione(): void {
  localStorage.removeItem(CHIAVE_SESSIONE)
}

/** Token da mettere in Authorization, disponibile fin dal primo render. */
export function tokenCorrente(): string | null {
  return leggiSessione()?.token ?? null
}
