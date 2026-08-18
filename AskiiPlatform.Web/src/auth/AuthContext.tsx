import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { authApi } from '@/api/endpoints'
import { configureClient } from '@/api/client'
import type { LoginResult, TfaAvailable } from '@/api/types'
import { AuthStatus, Roles } from '@/api/types'
import { expiresAt } from './jwt'
import { leggiSessione, rimuoviSessione, salvaSessione } from './sessione'
import type { Session } from './sessione'

export type { Session } from './sessione'

export type LogoutReason = 'utente' | 'scaduta'

/**
 * Esito del primo passaggio del login. Con la 2FA attiva la sessione non viene
 * stabilita: serve completare il secondo fattore con la sfida ricevuta.
 */
export type EsitoLogin =
  | { stato: 'completato' }
  | { stato: 'tfaRichiesta'; challengeToken: string; metodi: TfaAvailable[] }

interface AuthState {
  session: Session | null
  /** Motivo dell'ultimo logout involontario, da mostrare in pagina di login. */
  motivoUscita: LogoutReason | null
  isAuthenticated: boolean
  isAdmin: boolean
  login: (email: string, password: string) => Promise<EsitoLogin>
  /** Secondo passaggio: stabilisce la sessione con il token appena emesso. */
  completaTfa: (challengeToken: string, metodo: TfaAvailable, codice: string) => Promise<void>
  logout: (motivo?: LogoutReason) => void
  scadenza: Date | null
}

const AuthContext = createContext<AuthState | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(leggiSessione)
  const [motivoUscita, setMotivoUscita] = useState<LogoutReason | null>(null)

  const logout = useCallback((motivo: LogoutReason = 'utente') => {
    rimuoviSessione()
    setSession(null)
    setMotivoUscita(motivo === 'utente' ? null : motivo)
  }, [])

  useEffect(() => {
    configureClient({ onUnauthorized: () => logout('scaduta') })
  }, [logout])

  // Il token dura 8h e non è rinnovabile: alla scadenza si esce da soli,
  // senza aspettare il primo 401 su una chiamata.
  useEffect(() => {
    if (!session) return
    const scadenza = expiresAt(session.token)
    if (!scadenza) return

    const fraQuanto = scadenza.getTime() - Date.now()
    if (fraQuanto <= 0) {
      logout('scaduta')
      return
    }
    const timer = window.setTimeout(() => logout('scaduta'), fraQuanto)
    return () => window.clearTimeout(timer)
  }, [session, logout])

  /**
   * Traduce una risposta completa in sessione. Il backend garantisce che con
   * status = Ok i campi identificativi siano valorizzati, ma sono nullable nel
   * tipo perché la stessa risposta serve anche al caso TfaRequired: qui si
   * controlla, invece di forzare con `!`.
   */
  const stabilisciSessione = useCallback((res: LoginResult) => {
    if (!res.token || !res.userId || !res.email || !res.role) {
      throw new Error('Risposta di autenticazione incompleta')
    }

    const nuova: Session = {
      token: res.token,
      userId: res.userId,
      email: res.email,
      fullName: res.fullName ?? '',
      role: res.role,
    }

    salvaSessione(nuova)
    setMotivoUscita(null)
    setSession(nuova)
  }, [])

  const login = useCallback(
    async (email: string, password: string): Promise<EsitoLogin> => {
      const res = await authApi.login({ email, password })

      if (res.status === AuthStatus.TfaRequired) {
        if (!res.challengeToken) throw new Error('Sfida di verifica mancante nella risposta')
        return {
          stato: 'tfaRichiesta',
          challengeToken: res.challengeToken,
          metodi: res.tfaMethods ?? [],
        }
      }

      stabilisciSessione(res)
      return { stato: 'completato' }
    },
    [stabilisciSessione],
  )

  const completaTfa = useCallback(
    async (challengeToken: string, metodo: TfaAvailable, codice: string) => {
      stabilisciSessione(await authApi.verifyTfa({ challengeToken, method: metodo, code: codice }))
    },
    [stabilisciSessione],
  )

  const value = useMemo<AuthState>(
    () => ({
      session,
      motivoUscita,
      isAuthenticated: session !== null,
      isAdmin: session?.role === Roles.Admin,
      login,
      completaTfa,
      logout,
      scadenza: session ? expiresAt(session.token) : null,
    }),
    [session, motivoUscita, login, completaTfa, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth va usato dentro AuthProvider')
  return ctx
}
