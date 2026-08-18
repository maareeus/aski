import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { RequireAdmin, RequireAuth } from './auth/RequireAuth'
import { AppLayout } from './layout/AppLayout'
import { ActivateUserPage } from './pages/ActivateUserPage'
import { ChangePasswordPage } from './pages/ChangePasswordPage'
import { DashboardPage } from './pages/DashboardPage'
import { LoginPage } from './pages/LoginPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { ProfilePage } from './pages/ProfilePage'
import { UserCreatePage } from './pages/UserCreatePage'
import { UserDeletePage } from './pages/UserDeletePage'
import { UserUpdatePage } from './pages/UserUpdatePage'
import { UsersListPage } from './pages/UsersListPage'

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />

          {/* L'endpoint di attivazione è anonimo: la pagina resta fuori dall'area protetta */}
          <Route path="/attiva" element={<ActivateUserPage />} />

          <Route element={<RequireAuth />}>
            <Route element={<AppLayout />}>
              <Route index element={<DashboardPage />} />
              <Route path="profilo" element={<ProfilePage />} />
              <Route path="password" element={<ChangePasswordPage />} />
              <Route path="utenti/attiva" element={<ActivateUserPage />} />

              <Route path="utenti" element={<RequireAdmin />}>
                <Route index element={<UsersListPage />} />
                <Route path="nuovo" element={<UserCreatePage />} />
                <Route path="modifica" element={<UserUpdatePage />} />
                <Route path="elimina" element={<UserDeletePage />} />
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
