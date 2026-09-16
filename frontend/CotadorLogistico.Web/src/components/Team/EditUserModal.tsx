import { useEffect, useState } from 'react';
import { useI18n } from '../../contexts/I18nContext';
import { useToast } from '../common/Toast';
import { Modal } from '../common/Modal';
import { CustomSelect } from '../common/CustomSelect';
import { api, ApiError } from '../../lib/apiClient';
import type { AdminUserDto, SettingsStatusResponse, TeamMemberDto, UpdateUserAdminRequest } from '../../types/api';

interface EditUserModalProps {
  isOpen: boolean;
  onClose: () => void;
  member: TeamMemberDto;

  allMembers: TeamMemberDto[];
  ownerId: string;
  onUpdated: () => void;
}

const PROVIDERS: Array<{ key: 'frenet' | 'melhorenvio' | 'gemini'; labelKey: string; configuredKey: keyof SettingsStatusResponse }> = [
  { key: 'frenet', labelKey: 'team.edit.frenet', configuredKey: 'frenetConfigured' },
  { key: 'melhorenvio', labelKey: 'team.edit.melhorEnvio', configuredKey: 'melhorEnvioConfigured' },
  { key: 'gemini', labelKey: 'team.edit.gemini', configuredKey: 'geminiConfigured' },
];

export function EditUserModal({ isOpen, onClose, member, allMembers, ownerId, onUpdated }: EditUserModalProps) {
  const { t } = useI18n();
  const { showToast } = useToast();

  const [name, setName] = useState(member.name);
  const [email, setEmail] = useState(member.email ?? '');
  const [position, setPosition] = useState(member.position ?? '');
  const [role, setRole] = useState<'OPERATOR' | 'SUPERVISOR'>(member.role === 'SUPERVISOR' ? 'SUPERVISOR' : 'OPERATOR');
  const [supervisorId, setSupervisorId] = useState(member.supervisorId ?? ownerId);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [integrations, setIntegrations] = useState<SettingsStatusResponse | null>(null);
  const [temporaryPassword, setTemporaryPassword] = useState<string | null>(null);
  const [isBusy, setIsBusy] = useState(false);

  useEffect(() => {
    if (!isOpen) return;
    setName(member.name);
    setEmail(member.email ?? '');
    setPosition(member.position ?? '');
    setRole(member.role === 'SUPERVISOR' ? 'SUPERVISOR' : 'OPERATOR');
    setSupervisorId(member.supervisorId ?? ownerId);
    setTemporaryPassword(null);
    setError(null);

    api.get<SettingsStatusResponse>(`/api/admin/users/${member.id}/integrations`).then(setIntegrations).catch(() => setIntegrations(null));
  }, [isOpen, member.id]);

  const supervisorOptions = allMembers.filter((m) => m.role === 'SUPERVISOR' && m.id !== member.id);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setIsSaving(true);
    setError(null);

    try {
      const payload: UpdateUserAdminRequest = {
        name: name.trim() || null,
        position: position,
        email: email.trim() || null,
        role,
        supervisorId: role === 'SUPERVISOR' ? null : supervisorId,
      };
      await api.patch<AdminUserDto>(`/api/admin/users/${member.id}`, payload);
      showToast(t('team.edit.saveSuccess'));
      onUpdated();
      onClose();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('team.edit.saveError'));
    } finally {
      setIsSaving(false);
    }
  }

  async function handleToggleActive() {
    const next = !member.isActive;
    if (next === false && !window.confirm(t('team.edit.deactivateConfirm', { name: member.name }))) return;

    setIsBusy(true);
    try {
      await api.patch(`/api/admin/users/${member.id}/active`, { isActive: next });
      showToast(next ? t('team.edit.activateSuccess') : t('team.edit.deactivateSuccess'));
      onUpdated();
      onClose();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('team.edit.saveError'));
    } finally {
      setIsBusy(false);
    }
  }

  async function handleIssueTemporaryPassword() {
    if (!window.confirm(t('team.edit.temporaryPasswordConfirm', { name: member.name }))) return;

    setIsBusy(true);
    setError(null);
    try {
      const response = await api.post<{ temporaryPassword: string }>(`/api/admin/users/${member.id}/temporary-password`);
      setTemporaryPassword(response.temporaryPassword);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('team.edit.saveError'));
    } finally {
      setIsBusy(false);
    }
  }

  async function handleResetIntegration(provider: 'frenet' | 'melhorenvio' | 'gemini') {
    if (!window.confirm(t('team.edit.resetIntegrationConfirm', { name: member.name }))) return;

    setIsBusy(true);
    setError(null);
    try {
      const status = await api.post<SettingsStatusResponse>(`/api/admin/users/${member.id}/integrations/${provider}/reset`);
      setIntegrations(status);
      showToast(t('team.edit.resetIntegrationSuccess'));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('team.edit.saveError'));
    } finally {
      setIsBusy(false);
    }
  }

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={t('team.edit.title', { name: member.name })} maxWidth={520}>
      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 18 }}>
        <div className="input-group">
          <label htmlFor="editName">{t('team.edit.nameLabel')}</label>
          <input id="editName" type="text" required value={name} onChange={(e) => setName(e.target.value)} />
        </div>

        <div className="input-group">
          <label htmlFor="editEmail">{t('team.edit.emailLabel')}</label>
          <input id="editEmail" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        </div>

        <div className="input-group">
          <label htmlFor="editPosition">{t('team.edit.positionLabel')}</label>
          <input id="editPosition" type="text" value={position} onChange={(e) => setPosition(e.target.value)} />
        </div>

        <div className="input-group">
          <label id="editRoleLabel">{t('team.edit.roleLabel')}</label>
          <div className="segmented" role="group" aria-labelledby="editRoleLabel">
            <button type="button" className={`segmented-option ${role === 'OPERATOR' ? 'is-active' : ''}`} onClick={() => setRole('OPERATOR')}>
              {t('supervisor.roleOperator')}
            </button>
            <button type="button" className={`segmented-option ${role === 'SUPERVISOR' ? 'is-active' : ''}`} onClick={() => setRole('SUPERVISOR')}>
              {t('supervisor.roleSupervisor')}
            </button>
          </div>
        </div>

        {role === 'OPERATOR' && (
          <div className="input-group">
            <label id="editSupervisorLabel">{t('team.edit.supervisorLabel')}</label>
            <CustomSelect
              id="editSupervisor" aria-labelledby="editSupervisorLabel" value={supervisorId} onChange={setSupervisorId}
              options={[
                { value: ownerId, label: t('team.edit.supervisorOwnerOption') },
                ...supervisorOptions.map((s) => ({ value: s.id, label: s.name })),
              ]}
            />
          </div>
        )}

        {error && <div className="error-text">{error}</div>}

        <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
          <button type="button" className="btn-secondary" onClick={onClose}>{t('supervisor.cancelButton')}</button>
          <button type="submit" className="btn-primary" style={{ marginBottom: 0 }} disabled={isSaving}>
            {isSaving ? t('team.edit.saving') : t('team.edit.saveButton')}
          </button>
        </div>

        <hr className="settings-divider" />

        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          <div className="settings-section-title">{t('team.edit.statusSectionTitle')}</div>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 10 }}>
            <span>
              {member.isActive ? t('team.edit.statusActive') : t('team.edit.statusInactive')}
            </span>
            <button type="button" className="btn-secondary" disabled={isBusy} onClick={() => void handleToggleActive()}>
              {member.isActive ? t('team.edit.deactivateButton') : t('team.edit.activateButton')}
            </button>
          </div>
        </div>

        <hr className="settings-divider" />

        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          <div className="settings-section-title">{t('team.edit.passwordSectionTitle')}</div>
          <p className="field-hint">{t('team.edit.passwordHint')}</p>
          <button type="button" className="btn-secondary" disabled={isBusy} onClick={() => void handleIssueTemporaryPassword()}>
            {t('team.edit.issueTemporaryPasswordButton')}
          </button>
          {temporaryPassword && (
            <div className="notice-banner" style={{ display: 'block', fontFamily: 'monospace', wordBreak: 'break-all' }}>
              <strong>{temporaryPassword}</strong>
              <div className="field-hint" style={{ marginTop: 6 }}>{t('team.edit.temporaryPasswordWarning')}</div>
            </div>
          )}
        </div>

        <hr className="settings-divider" />

        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          <div className="settings-section-title">{t('team.edit.integrationsSectionTitle')}</div>
          {PROVIDERS.map(({ key, labelKey, configuredKey }) => (
            <div key={key} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 10 }}>
              <span>
                {t(labelKey)} —{' '}
                <span style={{ color: integrations?.[configuredKey] ? 'var(--info-text)' : 'var(--faint)' }}>
                  {integrations?.[configuredKey] ? t('settings.configuredLabel') : t('settings.notConfiguredLabel')}
                </span>
              </span>
              <button
                type="button" className="btn-secondary" disabled={isBusy || !integrations?.[configuredKey]}
                onClick={() => void handleResetIntegration(key)}
              >
                {t('team.edit.resetButton')}
              </button>
            </div>
          ))}
        </div>
      </form>
    </Modal>
  );
}
