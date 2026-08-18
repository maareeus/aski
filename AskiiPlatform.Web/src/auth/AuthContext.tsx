import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { authApi } from '@/api/endpoints'
import { configureClient } from '@/api/client'
import type { LoginResult } from '@/api/types'
import { Roles } from '@/api/types'
import { expiresAt } from './jwt'
import { leggiSessione, rimuoviSessione, salvaSessione } from './sessione'
import type { Session } from './sessione'

export type { Session } from './sessione'

export type LogoutReason = 'utente' | 'scaduta'

interface AuthState {
  session: Session | null
  /** Motivo dell'ultimo logout involontario, da mostrare in pagina di login. */
  motivoUscita: LogoutReason | null
  isAuthenticated: boolean
  isAdmin: boolean
  login: (email: string, password: string) => Promise<void>
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

  const login = useCallback(async (email: string, password: string) => {
    const res: LoginResult = await authApi.login({ email, password })
    const nuova: Session = {
      token: res.token,
      userId: res.userId,
      email: res.email,
      fullName: res.fullName,
      role: res.role,
    }
    salvaSessione(nuova)
    setMotivoUscita(null)
    setSession(nuova)
  }, [])

  const value = useMemo<AuthState>(
    () => ({
      session,
      motivoUscita,
      isAuthenticated: session !== null,
      isAdmin: session?.role === Roles.Admin,
      login,
      logout,
      scadenza: session ? expiresAt(session.token) : null,
    }),
    [session, motivoUscita, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth va usato dentro AuthProvider')
  return ctx
}
