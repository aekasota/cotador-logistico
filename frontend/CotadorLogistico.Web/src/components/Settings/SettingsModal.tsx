import { useEffect, useState } from 'react';
import { useI18n } from '../../contexts/I18nContext';
import { useToast } from '../common/Toast';
import { Modal } from '../common/Modal';
import { api, ApiError } from '../../lib/apiClient';
import { EyeIcon, EyeOffIcon } from '../common/Icons';
import type { SettingsStatusResponse, UpdateSettingsRequest } from '../../types/api';

interface SettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export function SettingsModal({ isOpen, onClose }: SettingsModalProps) {
  const { t, language } = useI18n();
  const { showToast } = useToast();

  const [status, setStatus] = useState<SettingsStatusResponse | null>(null);
  const [frenetToken, setFrenetToken] = useState('');
  const [meToken, setMeToken] = useState('');
  const [geminiKey, setGeminiKey] = useState('');
  const [showFrenet, setShowFrenet] = useState(false);
  const [showMe, setShowMe] = useState(false);
  const [showGemini, setShowGemini] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  useEffect(() => {
    if (!isOpen) return;
    setFeedback(null);
    api
      .get<SettingsStatusResponse>('/api/settings')
      .then(setStatus)
      .catch(() => setStatus(null));
  }, [isOpen]);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setIsSaving(true);
    setFeedback(null);

    try {
      const payload: UpdateSettingsRequest = {
        frenetToken: frenetToken.trim() || null,
        melhorEnvioToken: meToken.trim() || null,
        geminiApiKey: geminiKey.trim() || null,
      };
      const updated = await api.post<SettingsStatusResponse>('/api/settings', payload);
      setStatus(updated);
      setFrenetToken('');
      setMeToken('');
      setGeminiKey('');
      setFeedback({ type: 'success', text: t('settings.saveSuccess') });
      showToast(t('settings.saveSuccess'));
    } catch (err) {
      const message = err instanceof ApiError ? err.message : t('settings.saveError');
      setFeedback({ type: 'error', text: message });
    } finally {
      setIsSaving(false);
    }
  }

  function formatAudit(audit: { updatedAt: string; updatedByName: string | null } | null): string | null {
    if (!audit) return null;
    const date = new Date(audit.updatedAt).toLocaleDateString(language);
    return t('settings.auditUpdatedBy', { name: audit.updatedByName ?? '—', date });
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={t('settings.title')}
      maxWidth={480}
      footer={
        <>
          <button type="button" className="btn-secondary" onClick={onClose}>{t('settings.cancelButton')}</button>
          <button type="submit" form="settingsForm" className="btn-primary" style={{ marginBottom: 0 }} disabled={isSaving}>
            {t('settings.saveButton')}
          </button>
        </>
      }
    >
      <form id="settingsForm" onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 22 }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 18 }}>
          <div className="settings-section-title">{t('settings.apiSectionTitle')}</div>

          <TokenField
            id="frenetToken" label={t('settings.frenetTokenLabel')} hint={t('settings.frenetTokenHint')}
            configured={status?.frenetConfigured ?? false} audit={formatAudit(status?.frenetAudit ?? null)}
            placeholder={status?.frenetConfigured ? t('settings.frenetTokenPlaceholderSet') : t('settings.frenetTokenPlaceholder')}
            value={frenetToken} onChange={setFrenetToken} show={showFrenet} onToggleShow={() => setShowFrenet((v) => !v)}
          />

          <TokenField
            id="meToken" label={t('settings.meTokenLabel')} hint={t('settings.meTokenHint')} prefixBadge="Bearer"
            configured={status?.melhorEnvioConfigured ?? false} audit={formatAudit(status?.melhorEnvioAudit ?? null)}
            placeholder={status?.melhorEnvioConfigured ? t('settings.meTokenPlaceholderSet') : t('settings.meTokenPlaceholder')}
            value={meToken} onChange={setMeToken} show={showMe} onToggleShow={() => setShowMe((v) => !v)}
          />

          <div className="field-hint">{t('settings.keepBlankHint')}</div>
        </div>

        <hr className="settings-divider" />

        <div style={{ display: 'flex', flexDirection: 'column', gap: 18 }}>
          <div className="settings-section-title">{t('settings.geminiSectionTitle')}</div>
          <TokenField
            id="geminiKey" label={t('settings.geminiLabel')} hint={t('settings.geminiHint')}
            configured={status?.geminiConfigured ?? false} audit={formatAudit(status?.geminiAudit ?? null)}
            placeholder={status?.geminiConfigured ? t('settings.geminiPlaceholderSet') : t('settings.geminiPlaceholder')}
            value={geminiKey} onChange={setGeminiKey} show={showGemini} onToggleShow={() => setShowGemini((v) => !v)}
          />
        </div>

        {feedback && <div className={`settings-feedback is-${feedback.type}`}>{feedback.text}</div>}
      </form>
    </Modal>
  );
}

function TokenField({
  id, label, hint, configured, audit, placeholder, value, onChange, show, onToggleShow, prefixBadge,
}: {
  id: string; label: string; hint: string; configured: boolean; audit: string | null; placeholder: string;
  value: string; onChange: (v: string) => void; show: boolean; onToggleShow: () => void; prefixBadge?: string;
}) {
  const { t } = useI18n();
  return (
    <div className="input-group">
      <label htmlFor={id}>
        {label} — <span style={{ color: configured ? 'var(--info-text)' : 'var(--faint)' }}>
          {configured ? t('settings.configuredLabel') : t('settings.notConfiguredLabel')}
        </span>
      </label>
      <div className={prefixBadge ? 'input-with-prefix' : undefined}>
        {prefixBadge && <span className="input-prefix-badge">{prefixBadge}</span>}
        <div className="input-with-toggle">
          <input
            id={id} type={show ? 'text' : 'password'} autoComplete="off"
            placeholder={placeholder} value={value} onChange={(e) => onChange(e.target.value)}
          />
          <button type="button" className="password-toggle-btn" onClick={onToggleShow} aria-label="Mostrar ou ocultar">
            {show ? <EyeOffIcon /> : <EyeIcon />}
          </button>
        </div>
      </div>
      <div className="field-hint">{hint}</div>
      {audit && <div className="integration-audit">{audit}</div>}
    </div>
  );
}
