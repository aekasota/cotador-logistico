import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { api } from '../lib/apiClient';
import type { ExchangeRateSnapshot } from '../types/api';

export type Currency = 'BRL' | 'USD' | 'MXN';

const LOCAL_STORAGE_KEY = 'cotador-currency';
const LOCALE_BY_CURRENCY: Record<Currency, string> = { BRL: 'pt-BR', USD: 'en-US', MXN: 'es-MX' };

interface CurrencyContextValue {
  currency: Currency;
  setCurrency: (currency: Currency) => void;

  rates: Partial<Record<'USD' | 'MXN', number>>;
  ratesAvailable: boolean;

  convertFromBrl: (valueInBrl: number) => number;
  formatMoney: (valueInBrl: number) => string;
}

const CurrencyContext = createContext<CurrencyContextValue | undefined>(undefined);

export function CurrencyProvider({ children }: { children: ReactNode }) {
  const [currency, setCurrencyState] = useState<Currency>(() => {
    const stored = localStorage.getItem(LOCAL_STORAGE_KEY);
    return stored === 'USD' || stored === 'MXN' ? stored : 'BRL';
  });
  const [rates, setRates] = useState<Partial<Record<'USD' | 'MXN', number>>>({});
  const [ratesAvailable, setRatesAvailable] = useState(false);

  useEffect(() => {
    let isMounted = true;

    api
      .get<ExchangeRateSnapshot[]>('/api/exchange-rates')
      .then((snapshots) => {
        if (!isMounted) return;
        const next: Partial<Record<'USD' | 'MXN', number>> = {};
        for (const snapshot of snapshots) next[snapshot.currency] = snapshot.rateToBrl;
        setRates(next);
        setRatesAvailable(Object.keys(next).length > 0);
      })
      .catch(() => {
        if (!isMounted) return;
        setRatesAvailable(false);
      });

    return () => {
      isMounted = false;
    };
  }, []);

  const setCurrency = useCallback((next: Currency) => {
    setCurrencyState(next);
    localStorage.setItem(LOCAL_STORAGE_KEY, next);
  }, []);

  useEffect(() => {
    if (!ratesAvailable && currency !== 'BRL') setCurrency('BRL');
  }, [ratesAvailable, currency, setCurrency]);

  const convertFromBrl = useCallback(
    (valueInBrl: number) => {
      if (currency === 'BRL') return valueInBrl;
      const rate = rates[currency];
      if (!rate) return valueInBrl;
      return valueInBrl / rate;
    },
    [currency, rates],
  );

  const formatMoney = useCallback(
    (valueInBrl: number) => {
      const converted = convertFromBrl(valueInBrl);
      return converted.toLocaleString(LOCALE_BY_CURRENCY[currency], { style: 'currency', currency });
    },
    [convertFromBrl, currency],
  );

  const value = useMemo(
    () => ({ currency, setCurrency, rates, ratesAvailable, convertFromBrl, formatMoney }),
    [currency, setCurrency, rates, ratesAvailable, convertFromBrl, formatMoney],
  );

  return <CurrencyContext.Provider value={value}>{children}</CurrencyContext.Provider>;
}

export function useCurrency(): CurrencyContextValue {
  const context = useContext(CurrencyContext);
  if (!context) throw new Error('useCurrency precisa ser usado dentro de um CurrencyProvider.');
  return context;
}
