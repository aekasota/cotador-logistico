import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import ptBR from '../i18n/pt-BR.json';
import esMX from '../i18n/es-MX.json';
import enUS from '../i18n/en-US.json';
import { api } from '../lib/apiClient';
import { useAuth } from './AuthContext';

export const SUPPORTED_LANGUAGES = ['pt-BR', 'es-MX', 'en-US'] as const;
export type Language = (typeof SUPPORTED_LANGUAGES)[number];
const DEFAULT_LANGUAGE: Language = 'pt-BR';

type TranslationTree = Record<string, any>;
const TRANSLATIONS: Record<Language, TranslationTree> = { 'pt-BR': ptBR, 'es-MX': esMX, 'en-US': enUS };

const LOCAL_STORAGE_KEY = 'cotador-language';

function detectInitialLanguage(): Language {
  const stored = localStorage.getItem(LOCAL_STORAGE_KEY);
  if (stored && (SUPPORTED_LANGUAGES as readonly string[]).includes(stored)) return stored as Language;

  const browserLang = (navigator.language || DEFAULT_LANGUAGE).toLowerCase();
  if (browserLang.startsWith('es')) return 'es-MX';
  if (browserLang.startsWith('en')) return 'en-US';
  return DEFAULT_LANGUAGE;
}

interface I18nContextValue {
  language: Language;
  setLanguage: (language: Language) => void;
  t: (keyPath: string, vars?: Record<string, string | number>) => string;
}

const I18nContext = createContext<I18nContextValue | undefined>(undefined);

export function I18nProvider({ children }: { children: ReactNode }) {
  const { status, profile } = useAuth();
  const [language, setLanguageState] = useState<Language>(detectInitialLanguage);

  useEffect(() => {
    if (status === 'authenticated' && profile?.language && (SUPPORTED_LANGUAGES as readonly string[]).includes(profile.language)) {
      setLanguageState(profile.language as Language);
    }
  }, [status, profile?.language]);

  useEffect(() => {
    document.documentElement.lang = language;
    localStorage.setItem(LOCAL_STORAGE_KEY, language);
  }, [language]);

  const setLanguage = useCallback(
    (next: Language) => {
      setLanguageState(next);
      if (status === 'authenticated') {
        api.patch('/api/me/preferences', { theme: null, language: next }).catch(() => {});
      }
    },
    [status],
  );

  const t = useCallback(
    (keyPath: string, vars?: Record<string, string | number>) => {
      const parts = keyPath.split('.');

      let node: any = TRANSLATIONS[language];
      for (const part of parts) {
        if (node == null) return keyPath;
        node = node[part];
      }
      if (typeof node !== 'string') return keyPath;

      if (!vars) return node;
      return Object.keys(vars).reduce((text, key) => text.split(`{${key}}`).join(String(vars[key])), node as string);
    },
    [language],
  );

  const value = useMemo(() => ({ language, setLanguage, t }), [language, setLanguage, t]);

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

export function useI18n(): I18nContextValue {
  const context = useContext(I18nContext);
  if (!context) throw new Error('useI18n precisa ser usado dentro de um I18nProvider.');
  return context;
}
