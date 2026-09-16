import { useRef, useState } from 'react';
import { useI18n } from '../../contexts/I18nContext';
import { api, ApiError } from '../../lib/apiClient';
import { Popover } from '../common/Popover';
import { SparkleIcon } from '../common/Icons';
import type { PackageDimensionsResponse, PackageDimensionSuggestion } from '../../types/api';

interface AiAssistPopoverProps {
  onApply: (suggestion: PackageDimensionSuggestion) => void;
}

export function AiAssistPopover({ onApply }: AiAssistPopoverProps) {
  const { t } = useI18n();
  const [isOpen, setIsOpen] = useState(false);
  const [description, setDescription] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [result, setResult] = useState<PackageDimensionsResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);

  async function handleSubmit() {
    if (!description.trim()) return;
    setIsSubmitting(true);
    setError(null);
    setResult(null);

    try {
      const response = await api.post<PackageDimensionsResponse>('/api/ai/package-dimensions', {
        productDescription: description.trim(),
      });
      setResult(response);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('ai.refusedDefault'));
    } finally {
      setIsSubmitting(false);
    }
  }

  function handleClose() {
    setIsOpen(false);
    setDescription('');
    setResult(null);
    setError(null);
  }

  return (
    <>
      <button ref={triggerRef} type="button" className="ai-assist-btn" title={t('ai.triggerLabel')} aria-label={t('ai.triggerLabel')} onClick={() => setIsOpen(true)}>
        <SparkleIcon />
      </button>

      <Popover isOpen={isOpen} onClose={handleClose} anchorEl={triggerRef.current} title={t('ai.modalTitle')}>
        <p style={{ fontSize: 13, marginBottom: 12 }}>{t('ai.modalSubtitle')}</p>

        <div className="input-group" style={{ marginBottom: 12 }}>
          <label htmlFor="ai-product-description">{t('ai.descLabel')}</label>
          <textarea
            id="ai-product-description"
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            placeholder={t('ai.descPlaceholder')}
            maxLength={500}
            rows={3}
            style={{
              fontFamily: 'Sora', fontSize: 13, border: '1px solid var(--line)', borderRadius: 6,
              padding: 8, resize: 'vertical', background: 'transparent', color: 'var(--ink)',
            }}
          />
        </div>

        <button type="button" className="btn-primary" style={{ marginBottom: 14, width: '100%', justifyContent: 'center' }} disabled={isSubmitting || !description.trim()} onClick={() => void handleSubmit()}>
          {isSubmitting ? t('ai.submitting') : t('ai.submitButton')}
        </button>

        {error && <div className="error-text" style={{ marginBottom: 10 }}>{error}</div>}

        {result && !result.allowed && <div className="error-text">{result.warning}</div>}

        {result && result.allowed && (
          <div>
            <div className="popover-title">{t('ai.resultTitle')}</div>
            {result.suggestions.map((suggestion, index) => (
              <div className="ai-suggestion-row" key={index}>
                <div>
                  <div className="popover-row-main">{suggestion.lengthCm} × {suggestion.widthCm} × {suggestion.heightCm} cm</div>
                  <div className="popover-row-sub">{suggestion.reason}</div>
                  <div className="ai-confidence">{t(`ai.confidence${capitalize(suggestion.confidence)}`)}</div>
                </div>
                <button type="button" className="popover-select-btn" onClick={() => { onApply(suggestion); handleClose(); }}>
                  {t('ai.applyButton')}
                </button>
              </div>
            ))}
            <p className="field-hint" style={{ marginTop: 10 }}>{result.warning || t('ai.disclaimer')}</p>
          </div>
        )}
      </Popover>
    </>
  );
}

function capitalize(value: string): string {
  return value.charAt(0).toUpperCase() + value.slice(1);
}
