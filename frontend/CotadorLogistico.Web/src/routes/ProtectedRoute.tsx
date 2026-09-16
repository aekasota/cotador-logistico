import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { status, profile, isDemo } = useAuth();
  const location = useLocation();

  if (status === 'loading') return null;

  if (status === 'unauthenticated') {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  if (!isDemo && profile?.mustChangePassword && location.pathname !== '/change-password') {
    return <Navigate to="/change-password" replace />;
  }

  return <>{children}</>;
}

export function RequireSupervisorOrOwner({ children }: { children: ReactNode }) {
  const { profile, isDemo } = useAuth();

  if (isDemo || (profile && profile.role !== 'SUPERVISOR' && profile.role !== 'OWNER')) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
