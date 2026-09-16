import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import { ThemeProvider } from './contexts/ThemeContext';
import { I18nProvider } from './contexts/I18nContext';
import { CurrencyProvider } from './contexts/CurrencyContext';
import { ToastProvider } from './components/common/Toast';
import { SettingsModalProvider } from './contexts/SettingsModalContext';
import { AppLayout, NarrowAppLayout } from './components/common/AppLayout';
import { ProtectedRoute, RequireSupervisorOrOwner } from './routes/ProtectedRoute';
import { LoginPage } from './pages/LoginPage';
import { CotadorPage } from './pages/CotadorPage';
import { ProfilePage } from './pages/ProfilePage';
import { SupervisorPage } from './pages/SupervisorPage';
import { ChangePasswordPage } from './pages/ChangePasswordPage';

export default function App() {
  return (
    <AuthProvider>
      <ThemeProvider>
        <I18nProvider>
          <CurrencyProvider>
            <ToastProvider>
              <SettingsModalProvider>
                <BrowserRouter>
                  <Routes>
                    <Route path="/login" element={<LoginPage />} />

                    <Route
                      element={
                        <ProtectedRoute>
                          <AppLayout />
                        </ProtectedRoute>
                      }
                    >
                      <Route path="/" element={<CotadorPage />} />
                    </Route>

                    <Route
                      element={
                        <ProtectedRoute>
                          <NarrowAppLayout />
                        </ProtectedRoute>
                      }
                    >
                      <Route path="/profile" element={<ProfilePage />} />
                      <Route path="/change-password" element={<ChangePasswordPage />} />
                      <Route
                        path="/supervisor"
                        element={
                          <RequireSupervisorOrOwner>
                            <SupervisorPage />
                          </RequireSupervisorOrOwner>
                        }
                      />
                    </Route>

                    <Route path="*" element={<Navigate to="/" replace />} />
                  </Routes>
                </BrowserRouter>
              </SettingsModalProvider>
            </ToastProvider>
          </CurrencyProvider>
        </I18nProvider>
      </ThemeProvider>
    </AuthProvider>
  );
}
