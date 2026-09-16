import { createContext, useContext, useState, type ReactNode } from 'react';
import { SettingsModal } from '../components/Settings/SettingsModal';

interface SettingsModalContextValue {
  openSettings: () => void;
}

const SettingsModalContext = createContext<SettingsModalContextValue | undefined>(undefined);

export function SettingsModalProvider({ children }: { children: ReactNode }) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <SettingsModalContext.Provider value={{ openSettings: () => setIsOpen(true) }}>
      {children}
      <SettingsModal isOpen={isOpen} onClose={() => setIsOpen(false)} />
    </SettingsModalContext.Provider>
  );
}

export function useSettingsModal(): SettingsModalContextValue {
  const context = useContext(SettingsModalContext);
  if (!context) throw new Error('useSettingsModal precisa ser usado dentro de um SettingsModalProvider.');
  return context;
}
