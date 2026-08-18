import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { Alert } from 'design-react-kit'
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
      <Alert color="danger">
        <h4 className="alert-heading h6">Accesso non consentito</h4>
        <p className="mb-0">
          Questa sezione è riservata al ruolo <strong>Admin</strong>. Il tuo ruolo attuale è{' '}
          <strong>{session?.role}</strong>.
        </p>
      </Alert>
    )
  }
  return <Outlet />
}
