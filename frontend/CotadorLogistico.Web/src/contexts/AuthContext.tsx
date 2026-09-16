import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import type { Session } from '@supabase/supabase-js';
import { supabase } from '../lib/supabaseClient';
import { api, setDemoModeActive } from '../lib/apiClient';
import type { ProfileResponse } from '../types/api';

export const DEMO_MODE_TRIGGER = '--demomode';

const DEMO_SESSION_STORAGE_KEY = 'cotador-demo-session';

type AuthStatus = 'loading' | 'authenticated' | 'demo' | 'unauthenticated';

interface AuthContextValue {
  status: AuthStatus;
  session: Session | null;
  profile: ProfileResponse | null;
  isDemo: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  signOut: () => Promise<void>;
  refreshProfile: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function buildDemoProfile(): ProfileResponse {
  return {
    id: 'demo',
    name: 'Sessão Demonstração',
    position: null,
    email: null,
    role: 'OPERATOR',
    theme: 'light',
    language: 'pt-BR',
    totalQuotes: 0,
    teamTotalQuotes: null,
    mustChangePassword: false,
  };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>('loading');
  const [session, setSession] = useState<Session | null>(null);
  const [profile, setProfile] = useState<ProfileResponse | null>(null);

  const loadProfile = useCallback(async () => {
    try {
      const response = await api.get<ProfileResponse>('/api/me');
      setProfile(response);
    } catch {
      setProfile(null);
    }
  }, []);

  useEffect(() => {
    let isMounted = true;

    async function init() {
      const wasDemo = sessionStorage.getItem(DEMO_SESSION_STORAGE_KEY) === '1';
      if (wasDemo) {
        setDemoModeActive(true);
        if (isMounted) {
          setProfile(buildDemoProfile());
          setStatus('demo');
        }
        return;
      }

      const { data } = await supabase.auth.getSession();
      if (!isMounted) return;

      if (data.session) {
        setSession(data.session);
        setStatus('authenticated');
        await loadProfile();
      } else {
        setStatus('unauthenticated');
      }
    }

    void init();

    const { data: subscription } = supabase.auth.onAuthStateChange((_event, newSession) => {
      if (sessionStorage.getItem(DEMO_SESSION_STORAGE_KEY) === '1') return;

      setSession(newSession);
      if (newSession) {
        setStatus('authenticated');
        void loadProfile();
      } else {
        setStatus('unauthenticated');
        setProfile(null);
      }
    });

    return () => {
      isMounted = false;
      subscription.subscription.unsubscribe();
    };
  }, [loadProfile]);

  const signIn = useCallback(
    async (email: string, password: string) => {
      if (email.trim() === DEMO_MODE_TRIGGER) {
        sessionStorage.setItem(DEMO_SESSION_STORAGE_KEY, '1');
        setDemoModeActive(true);
        setProfile(buildDemoProfile());
        setStatus('demo');
        return;
      }

      sessionStorage.removeItem(DEMO_SESSION_STORAGE_KEY);
      setDemoModeActive(false);

      const { data, error } = await supabase.auth.signInWithPassword({ email, password });
      if (error) throw error;

      setSession(data.session);
      setStatus('authenticated');
      await loadProfile();
    },
    [loadProfile],
  );

  const signOut = useCallback(async () => {
    sessionStorage.removeItem(DEMO_SESSION_STORAGE_KEY);
    setDemoModeActive(false);

    if (status === 'demo') {
      setProfile(null);
      setStatus('unauthenticated');
      return;
    }

    await supabase.auth.signOut();
    setSession(null);
    setProfile(null);
    setStatus('unauthenticated');
  }, [status]);

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      session,
      profile,
      isDemo: status === 'demo',
      signIn,
      signOut,
      refreshProfile: loadProfile,
    }),
    [status, session, profile, signIn, signOut, loadProfile],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth precisa ser usado dentro de um AuthProvider.');
  return context;
}
