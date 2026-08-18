import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { authApi } from '../api/endpoints'
import { configureClient } from '../api/client'
import type { LoginResult, Role } from '../api/types'
import { Roles } from '../api/types'
import { expiresAt, isExpired } from './jwt'

const STORAGE_KEY = 'askii.session'

export interface Session {
  token: string
  userId: string
  email: string
  fullName: string
  role: Role
}

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

function leggiSessione(): Session | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return null
  try {
    const s = JSON.parse(raw) as Session
    if (!s.token || isExpired(s.token)) {
      localStorage.removeItem(STORAGE_KEY)
      return null
    }
    return s
  } catch {
    localStorage.removeItem(STORAGE_KEY)
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(leggiSessione)
  const [motivoUscita, setMotivoUscita] = useState<LogoutReason | null>(null)

  // Il client legge il token da qui: con una ref evita di riconfigurarsi a ogni render.
  const sessionRef = useRef(session)
  sessionRef.current = session

  const logout = useCallback((motivo: LogoutReason = 'utente') => {
    localStorage.removeItem(STORAGE_KEY)
    setSession(null)
    setMotivoUscita(motivo === 'utente' ? null : motivo)
  }, [])

  useEffect(() => {
    configureClient({
      readToken: () => sessionRef.current?.token ?? null,
      onUnauthorized: () => logout('scaduta'),
    })
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
    localStorage.setItem(STORAGE_KEY, JSON.stringify(nuova))
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
