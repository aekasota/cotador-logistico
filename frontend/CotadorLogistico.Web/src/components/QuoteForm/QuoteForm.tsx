import { useState } from 'react';
import { useI18n } from '../../contexts/I18nContext';
import { formatFlexibleDecimal, maskCep, parseFlexibleDecimal } from '../../lib/format';
import { AiAssistPopover } from '../Ai/AiAssistPopover';
import { EraserIcon } from '../common/Icons';
import type { PackageDimensionSuggestion } from '../../types/api';

export interface CargoFormState {
  sellerCep: string;
  invoiceValue: string;
  weight: string;
  length: string;
  height: string;
  width: string;
}

const EMPTY_CARGO_FORM: CargoFormState = { sellerCep: '', invoiceValue: '', weight: '', length: '', height: '', width: '' };

interface QuoteFormProps {
  form: CargoFormState;
  onChange: (next: CargoFormState) => void;
  compareEnabled: boolean;
  compareLocked: boolean;
  onCompareChange: (enabled: boolean) => void;
  manualCepsEnabled: boolean;
  onManualCepsChange: (enabled: boolean) => void;
  manualCeps: string[];
  onManualCepsListChange: (ceps: string[]) => void;
}

export function QuoteForm({
  form, onChange, compareEnabled, compareLocked, onCompareChange,
  manualCepsEnabled, onManualCepsChange, manualCeps, onManualCepsListChange,
}: QuoteFormProps) {
  const { t } = useI18n();
  const [cepQuantity, setCepQuantity] = useState(Math.max(1, manualCeps.length || 1));

  function updateField<K extends keyof CargoFormState>(key: K, value: string) {
    onChange({ ...form, [key]: value });
  }

  function handleBlurDecimal(key: keyof CargoFormState, decimals: number) {
    return () => {
      const parsed = parseFlexibleDecimal(form[key]);
      if (!Number.isNaN(parsed)) updateField(key, formatFlexibleDecimal(parsed, decimals));
    };
  }

  function handleQuantityChange(value: number) {
    const qty = Math.min(Math.max(1, value || 1), 50);
    setCepQuantity(qty);
    const next = Array.from({ length: qty }, (_, i) => manualCeps[i] ?? '');
    onManualCepsListChange(next);
  }

  function applyAiSuggestion(suggestion: PackageDimensionSuggestion) {
    onChange({
      ...form,
      length: formatFlexibleDecimal(suggestion.lengthCm, 0),
      width: formatFlexibleDecimal(suggestion.widthCm, 0),
      height: formatFlexibleDecimal(suggestion.heightCm, 0),
    });
  }

  const isFormEmpty = Object.values(form).every((value) => value === '');

  return (
    <>
      <div className="cargo-section-header">
        <span className="settings-section-title">{t('cargo.sectionTitle')}</span>
        {!isFormEmpty && (
          <button type="button" className="export-btn" onClick={() => onChange(EMPTY_CARGO_FORM)}>
            <EraserIcon />
            {t('cargo.clearButton')}
          </button>
        )}
      </div>

      <div className="formgrid">
        <div className="f">
          <label htmlFor="sellerCep">{t('cargo.originCep')}</label>
          <input
            id="sellerCep"
            className="cep"
            type="text"
            inputMode="numeric"
            value={form.sellerCep}
            onChange={(e) => updateField('sellerCep', maskCep(e.target.value))}
          />
        </div>

        <div className="f">
          <label htmlFor="invoiceValue">{t('cargo.invoiceValue')}</label>
          <div className="affixed">
            <span className="affix">R$</span>
            <input
              id="invoiceValue"
              className="money"
              type="text"
              inputMode="decimal"
              value={form.invoiceValue}
              onChange={(e) => updateField('invoiceValue', e.target.value)}
              onBlur={handleBlurDecimal('invoiceValue', 2)}
            />
          </div>
        </div>

        <div className="f">
          <label htmlFor="weight">{t('cargo.weight')}</label>
          <div className="affixed">
            <input
              id="weight"
              className="weight"
              type="text"
              inputMode="decimal"
              value={form.weight}
              onChange={(e) => updateField('weight', e.target.value)}
              onBlur={handleBlurDecimal('weight', 1)}
            />
            <span className="affix">kg</span>
          </div>
        </div>

        <div className="dimgroup">
          <div className="f">
            <label htmlFor="length">{t('cargo.length')}</label>
            <div className="affixed">
              <input id="length" className="dim" type="text" inputMode="decimal" value={form.length} onChange={(e) => updateField('length', e.target.value)} onBlur={handleBlurDecimal('length', 0)} />
              <span className="affix">cm</span>
            </div>
          </div>
          <div className="f">
            <label htmlFor="height">{t('cargo.height')}</label>
            <div className="affixed">
              <input id="height" className="dim" type="text" inputMode="decimal" value={form.height} onChange={(e) => updateField('height', e.target.value)} onBlur={handleBlurDecimal('height', 0)} />
              <span className="affix">cm</span>
            </div>
          </div>
          <div className="f">
            <label htmlFor="width">{t('cargo.width')}</label>
            <div className="affixed">
              <input id="width" className="dim" type="text" inputMode="decimal" value={form.width} onChange={(e) => updateField('width', e.target.value)} onBlur={handleBlurDecimal('width', 0)} />
              <span className="affix">cm</span>
            </div>
          </div>

          <AiAssistPopover onApply={applyAiSuggestion} />
        </div>
      </div>

      <div className="tglrow">
        <label className={`toggle-row ${compareLocked ? 'is-locked' : ''} ${compareEnabled ? 'is-on' : ''}`}>
          <div>
            <span className="toggle-label">{t('options.compareTitle')}</span>
            <div className="toggle-desc">{compareLocked ? t('options.compareLockedDesc') : t('options.compareDesc')}</div>
          </div>
          <input type="checkbox" checked={compareEnabled} disabled={compareLocked} onChange={(e) => onCompareChange(e.target.checked)} className="visually-hidden" />
        </label>

        <label className={`toggle-row ${manualCepsEnabled ? 'is-on' : ''}`}>
          <div>
            <span className="toggle-label">{t('options.manualTitle')}</span>
            <div className="toggle-desc">{t('options.manualDesc')}</div>
          </div>
          <input type="checkbox" checked={manualCepsEnabled} onChange={(e) => onManualCepsChange(e.target.checked)} className="visually-hidden" />
        </label>
      </div>

      {manualCepsEnabled && (
        <div className="manual-ceps-container">
          <div className="input-group" style={{ marginBottom: 16 }}>
            <label id="cepQuantityLabel">{t('options.quantityLabel')}</label>
            <div className="stepper" role="group" aria-labelledby="cepQuantityLabel">
              <button
                type="button" className="stepper-btn" disabled={cepQuantity <= 1}
                onClick={() => handleQuantityChange(cepQuantity - 1)} aria-label={t('options.quantityDecrease')}
              >
                −
              </button>
              <span className="stepper-value">{cepQuantity}</span>
              <button
                type="button" className="stepper-btn" disabled={cepQuantity >= 50}
                onClick={() => handleQuantityChange(cepQuantity + 1)} aria-label={t('options.quantityIncrease')}
              >
                +
              </button>
            </div>
          </div>
          <div className="grid-auto">
            {Array.from({ length: cepQuantity }, (_, i) => (
              <div className="input-group" key={i}>
                <input
                  type="text"
                  placeholder={`CEP ${i + 1}`}
                  maxLength={9}
                  value={manualCeps[i] ?? ''}
                  onChange={(e) => {
                    const next = [...manualCeps];
                    next[i] = maskCep(e.target.value);
                    onManualCepsListChange(next);
                  }}
                />
              </div>
            ))}
          </div>
        </div>
      )}
    </>
  );
}
