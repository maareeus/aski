import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { ShieldX } from 'lucide-react'
import { Esito } from '@/ui/Esito'
import { PageHeader } from '@/ui/PageHeader'
import { useAuth } from './AuthContext'

/** Consente l'accesso solo se autenticati, ricordando la pagina richiesta. */
export function RequireAuth() {
  const { isAuthenticated } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }
  return <Outlet />
}

/**
 * Solo per gli Admin. Non reindirizza al login: l'utente è autenticato,
 * semplicemente non ha i permessi, e mascherarlo con un redirect confonde.
 */
export function RequireAdmin() {
  const { isAdmin, session } = useAuth()

  if (!isAdmin) {
    return (
      <>
        <PageHeader titolo="Accesso non consentito" />
        <div className="max-w-2xl">
          <Esito
            tono="errore"
            titolo={
              <span className="flex items-center gap-2">
                <ShieldX className="size-4" />
                Permessi insufficienti
              </span>
            }
          >
            Questa sezione è riservata al ruolo <strong>Admin</strong>. Il tuo ruolo attuale è{' '}
            <strong>{session?.role}</strong>.
          </Esito>
        </div>
      </>
    )
  }
  return <Outlet />
}
