import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from '@/auth/AuthContext'
import { RequireAdmin, RequireAuth } from '@/auth/RequireAuth'
import { AppLayout } from '@/layout/AppLayout'
import { ActivateUserPage } from '@/pages/ActivateUserPage'
import { ChangePasswordPage } from '@/pages/ChangePasswordPage'
import { DashboardPage } from '@/pages/DashboardPage'
import { LoginPage } from '@/pages/LoginPage'
import { NotFoundPage } from '@/pages/NotFoundPage'
import { ProfilePage } from '@/pages/ProfilePage'
import { UsersListPage } from '@/pages/UsersListPage'
import { UserCreatePage } from '@/pages/users/UserCreatePage'
import { UserDetailPage } from '@/pages/users/UserDetailPage'

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />

          {/* L'endpoint di attivazione è anonimo: la pagina resta fuori
              dall'area protetta, per l'utente che attiva il proprio account. */}
          <Route path="/attiva" element={<ActivateUserPage />} />

          <Route element={<RequireAuth />}>
            <Route element={<AppLayout />}>
              <Route index element={<DashboardPage />} />
              <Route path="profilo" element={<ProfilePage />} />
              <Route path="password" element={<ChangePasswordPage />} />

              <Route path="utenti" element={<RequireAdmin />}>
                <Route index element={<UsersListPage />} />
                <Route path="nuovo" element={<UserCreatePage />} />
                {/* Il segmento statico "nuovo" vince comunque sul dinamico:
                    react-router ordina per specificità, non per dichiarazione. */}
                <Route path=":id" element={<UserDetailPage />} />
              </Route>

              <Route path="*" element={<NotFoundPage />} />
            </Route>
          </Route>

          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
