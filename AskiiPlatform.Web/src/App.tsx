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
import { SettingsPage } from '@/pages/SettingsPage'
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
          <Route path="/activate" element={<ActivateUserPage />} />

          <Route element={<RequireAuth />}>
            <Route element={<AppLayout />}>
              <Route index element={<DashboardPage />} />
              <Route path="profile" element={<ProfilePage />} />
              <Route path="password" element={<ChangePasswordPage />} />

              <Route path="settings" element={<RequireAdmin />}>
                <Route index element={<SettingsPage />} />
              </Route>

              <Route path="users" element={<RequireAdmin />}>
                <Route index element={<UsersListPage />} />
                <Route path="new" element={<UserCreatePage />} />
                {/* Il segmento statico "new" vince comunque sul dinamico:
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
