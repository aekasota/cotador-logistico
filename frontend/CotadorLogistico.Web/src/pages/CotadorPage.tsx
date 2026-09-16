import { useEffect, useState } from 'react';
import { useI18n } from '../contexts/I18nContext';
import { useAuth } from '../contexts/AuthContext';
import { useCurrency } from '../contexts/CurrencyContext';
import { useSettingsModal } from '../contexts/SettingsModalContext';
import { usePresenceHeartbeat } from '../hooks/usePresenceHeartbeat';
import { useToast } from '../components/common/Toast';
import { QuoteForm, type CargoFormState } from '../components/QuoteForm/QuoteForm';
import { ResultCard } from '../components/Results/ResultCard';
import { SummaryPanel } from '../components/Results/SummaryPanel';
import { AdvantageChart } from '../components/Results/AdvantageChart';
import { ExportBar } from '../components/Export/ExportBar';
import { RestartIcon } from '../components/common/Icons';
import { api } from '../lib/apiClient';
import { CAPITAIS } from '../lib/capitais';
import { buildQuoteResult, fetchFrenet, fetchMelhorEnvio, type Destination, type NormalizedOption, type QuoteResult } from '../lib/quoting';
import { buildPersistQuoteRequest } from '../lib/persistQuote';
import type { CapitalAdvantage, SettingsStatusResponse } from '../types/api';

const EMPTY_FORM: CargoFormState = { sellerCep: '', invoiceValue: '', weight: '', length: '', height: '', width: '' };

export function CotadorPage() {
  const { t } = useI18n();
  const { isDemo } = useAuth();
  const { openSettings } = useSettingsModal();
  const { currency, rates } = useCurrency();
  const { showToast } = useToast();

  const [form, setForm] = useState<CargoFormState>(EMPTY_FORM);
  const [compareEnabled, setCompareEnabled] = useState(false);
  const [manualCepsEnabled, setManualCepsEnabled] = useState(false);
  const [manualCeps, setManualCeps] = useState<string[]>(['']);

  const [frenetConfigured, setFrenetConfigured] = useState(false);
  const [meConfigured, setMeConfigured] = useState(false);

  const [isLoading, setIsLoading] = useState(false);
  const [progress, setProgress] = useState({ current: 0, total: 0 });
  const [results, setResults] = useState<QuoteResult[]>([]);
  const [resultsCompareMode, setResultsCompareMode] = useState(false);
  const [advantages, setAdvantages] = useState<CapitalAdvantage[]>([]);
  const [isClearingResults, setIsClearingResults] = useState(false);

  usePresenceHeartbeat(isLoading);

  useEffect(() => {
    if (isDemo) {
      setFrenetConfigured(true);
      setMeConfigured(true);
      return;
    }

    api
      .get<SettingsStatusResponse>('/api/settings')
      .then((status) => {
        setFrenetConfigured(status.frenetConfigured);
        setMeConfigured(status.melhorEnvioConfigured);
      })
      .catch(() => {
        setFrenetConfigured(false);
        setMeConfigured(false);
      });
  }, [isDemo]);

  useEffect(() => {
    if (!meConfigured && compareEnabled) setCompareEnabled(false);
  }, [meConfigured, compareEnabled]);

  function handleSelectOption(resultIndex: number, provider: 'FRENET' | 'MELHOR_ENVIO', option: NormalizedOption) {
    setResults((prev) =>
      prev.map((result, index) => {
        if (index !== resultIndex) return result;
        return provider === 'FRENET' ? { ...result, selectedFrenet: option } : { ...result, selectedMe: option };
      }),
    );
  }

  async function handleCalculate() {
    const sellerCep = form.sellerCep.replace(/\D/g, '');
    if (!sellerCep || sellerCep.length !== 8) {
      showToast(t('cargo.originCep') + ': CEP inválido.', true);
      return;
    }

    let destinations: Destination[];
    if (manualCepsEnabled) {
      destinations = manualCeps
        .map((cep) => cep.replace(/\D/g, ''))
        .filter((cep) => cep.length === 8)
        .map((cep) => ({ cep, label: 'Destino Manual' }));
      if (destinations.length === 0) {
        showToast('Preencha ao menos um CEP válido.', true);
        return;
      }
    } else {
      destinations = CAPITAIS;
    }

    const base = {
      sellerCep,
      invoice: parseFloat(form.invoiceValue.replace(',', '.')) || 0,
      weight: parseFloat(form.weight.replace(',', '.')) || 0,
      length: parseFloat(form.length.replace(',', '.')) || 0,
      height: parseFloat(form.height.replace(',', '.')) || 0,
      width: parseFloat(form.width.replace(',', '.')) || 0,
    };

    setIsLoading(true);
    setResults([]);
    setAdvantages([]);
    setResultsCompareMode(compareEnabled);
    setProgress({ current: 0, total: destinations.length });

    const collected: QuoteResult[] = [];

    for (const destination of destinations) {
      setProgress((p) => ({ ...p, current: collected.length }));

      if (compareEnabled) {
        const [frenetOptions, meOptions] = await Promise.all([
          fetchFrenet(base, destination, isDemo),
          fetchMelhorEnvio(base, destination, isDemo),
        ]);
        collected.push(buildQuoteResult(destination, frenetOptions, meOptions));
      } else {
        const frenetOptions = await fetchFrenet(base, destination, isDemo);
        collected.push(buildQuoteResult(destination, frenetOptions, null));
      }

      setResults([...collected]);
    }

    setProgress((p) => ({ ...p, current: destinations.length }));
    setIsLoading(false);

    if (!isDemo) {
      const exchangeRateUsed = currency === 'BRL' ? null : rates[currency] ?? null;
      await Promise.all(
        collected.map((result) =>
          api
            .post('/api/quotes', buildPersistQuoteRequest(result, form, compareEnabled, currency, exchangeRateUsed))
            .catch(() => {
            }),
        ),
      );

      if (compareEnabled) {
        try {
          const data = await api.get<CapitalAdvantage[]>('/api/quotes/metrics/price-advantage');
          setAdvantages(data);
        } catch {
          setAdvantages([]);
        }
      }
    }
  }

  function handleRestart() {
    setIsClearingResults(true);
    window.setTimeout(() => {
      setResults([]);
      setAdvantages([]);
      setResultsCompareMode(false);
      setProgress({ current: 0, total: 0 });
      setIsClearingResults(false);
    }, 240);
  }

  const cargoBase = {
    sellerCep: form.sellerCep.replace(/\D/g, ''),
    invoice: parseFloat(form.invoiceValue.replace(',', '.')) || 0,
    weight: parseFloat(form.weight.replace(',', '.')) || 0,
    length: parseFloat(form.length.replace(',', '.')) || 0,
    height: parseFloat(form.height.replace(',', '.')) || 0,
    width: parseFloat(form.width.replace(',', '.')) || 0,
  };

  return (
    <div className="page-enter">
      <QuoteForm
        form={form}
        onChange={setForm}
        compareEnabled={compareEnabled}
        compareLocked={!meConfigured}
        onCompareChange={setCompareEnabled}
        manualCepsEnabled={manualCepsEnabled}
        onManualCepsChange={setManualCepsEnabled}
        manualCeps={manualCeps}
        onManualCepsListChange={setManualCeps}
      />

      {!frenetConfigured && (
        <div className="notice-banner">
          {t('frenetNoticeSelf')}{' '}
          <button type="button" className="notice-banner-link" onClick={openSettings}>
            {t('frenetNoticeSelfLink')}
          </button>
        </div>
      )}

      <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 40 }}>
        <button type="button" className="btn-primary" style={{ marginBottom: 0 }} disabled={!frenetConfigured || isLoading} onClick={() => void handleCalculate()}>
          {t('calculate.button')}
        </button>

        {!isLoading && results.length > 0 && (
          <button type="button" className="icon-btn restart-btn" title={t('calculate.restartButton')} aria-label={t('calculate.restartButton')} onClick={handleRestart}>
            <RestartIcon />
          </button>
        )}
      </div>

      {isLoading && <div className="loading">{t('calculate.loadingProgress', { current: progress.current, total: progress.total })}</div>}

      <div className={`results-block ${isClearingResults ? 'is-clearing' : ''}`}>
        <div className={`results-container ${resultsCompareMode ? 'grid-compare' : 'grid-single'}`}>
          {results.map((result, index) => (
            <ResultCard
              key={`${result.destination.cep}-${index}`}
              result={result}
              comparisonMode={resultsCompareMode}
              onSelectOption={(provider, option) => handleSelectOption(index, provider, option)}
            />
          ))}
        </div>

        {!isLoading && resultsCompareMode && results.length > 0 && <SummaryPanel results={results} />}

        {!isLoading && !isDemo && resultsCompareMode && advantages.length > 0 && <AdvantageChart advantages={advantages} />}

        <ExportBar results={results} base={cargoBase} comparisonMode={resultsCompareMode} />
      </div>
    </div>
  );
}
