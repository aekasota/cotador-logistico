import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useI18n } from '../contexts/I18nContext';
import { useAuth } from '../contexts/AuthContext';
import { api, ApiError } from '../lib/apiClient';

export function ChangePasswordPage() {
  const { t } = useI18n();
  const { refreshProfile, profile } = useAuth();
  const navigate = useNavigate();

  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);

    if (newPassword.length < 8) {
      setError(t('changePassword.tooShort'));
      return;
    }
    if (newPassword !== confirmPassword) {
      setError(t('changePassword.mismatch'));
      return;
    }

    setIsSaving(true);
    try {
      await api.post('/api/me/change-password', { newPassword });
      await refreshProfile();
      navigate('/', { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('changePassword.error'));
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <form className="modal-card modal page-enter" style={{ maxWidth: 420, margin: '0 auto' }} onSubmit={handleSubmit}>
      <div className="modal-header">
        <span className="modal-title">{t('changePassword.title')}</span>
      </div>

      <div className="modal-body">
        <p className="field-hint">
          {t('changePassword.explanation', { name: profile?.name ?? '' })}
        </p>

        <div className="input-group">
          <label htmlFor="newPassword">{t('changePassword.newPasswordLabel')}</label>
          <input
            id="newPassword" type="password" required minLength={8} autoComplete="new-password"
            value={newPassword} onChange={(e) => setNewPassword(e.target.value)}
          />
        </div>

        <div className="input-group">
          <label htmlFor="confirmPassword">{t('changePassword.confirmPasswordLabel')}</label>
          <input
            id="confirmPassword" type="password" required minLength={8} autoComplete="new-password"
            value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)}
          />
        </div>

        {error && <div className="error-text">{error}</div>}
      </div>

      <div className="modal-footer">
        <button type="submit" className="btn-primary" style={{ marginBottom: 0 }} disabled={isSaving}>
          {isSaving ? t('changePassword.saving') : t('changePassword.saveButton')}
        </button>
      </div>
    </form>
  );
}
