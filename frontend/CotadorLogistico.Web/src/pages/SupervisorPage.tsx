import { useEffect, useState } from 'react';
import { useI18n } from '../contexts/I18nContext';
import { useAuth } from '../contexts/AuthContext';
import { Modal } from '../components/common/Modal';
import { CustomSelect } from '../components/common/CustomSelect';
import { EditUserModal } from '../components/Team/EditUserModal';
import { api, ApiError } from '../lib/apiClient';
import type { CreateTeamUserRequest, TeamMemberDto, TeamResponse } from '../types/api';

const PRESENCE_CLASS: Record<string, string> = { ONLINE: 'online', QUOTING: 'quoting', OFFLINE: 'offline' };
const PRESENCE_LABEL_KEY: Record<string, string> = { ONLINE: 'supervisor.statusOnline', QUOTING: 'supervisor.statusQuoting', OFFLINE: 'supervisor.statusOffline' };
const ROLE_LABEL_KEY: Record<string, string> = { OPERATOR: 'supervisor.roleOperator', SUPERVISOR: 'supervisor.roleSupervisor' };

export function SupervisorPage() {
  const { t } = useI18n();
  const { profile } = useAuth();
  const [team, setTeam] = useState<TeamResponse | null>(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [editingMember, setEditingMember] = useState<TeamMemberDto | null>(null);

  const isOwner = profile?.role === 'OWNER';

  async function loadTeam(query?: string) {
    try {
      const qs = query ? `?query=${encodeURIComponent(query)}` : '';
      const response = await api.get<TeamResponse>(`/api/team${qs}`);
      setTeam(response);
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadTeam(search);

    const interval = setInterval(() => void loadTeam(search), 30_000);
    return () => clearInterval(interval);
  }, [search]);

  return (
    <div className="page-enter">
      <div className="team-header-actions">
        <div className="team-summary">
          <div>
            <div className="team-stat-label">{t('supervisor.totalMembersLabel')}</div>
            <div className="team-stat-value">{team?.totalMembers ?? '—'}</div>
          </div>
          <div>
            <div className="team-stat-label">{t('supervisor.totalQuotesLabel')}</div>
            <div className="team-stat-value">{team?.totalQuotes ?? '—'}</div>
          </div>
        </div>
        <button type="button" className="btn-primary" style={{ marginBottom: 0 }} onClick={() => setIsCreateOpen(true)}>
          {t('supervisor.createUserButton')}
        </button>
      </div>

      <div className="input-group" style={{ marginBottom: 16 }}>
        <input
          type="search" placeholder={t('supervisor.searchPlaceholder')} value={search}
          onChange={(e) => setSearch(e.target.value)} aria-label={t('supervisor.searchPlaceholder')}
        />
      </div>

      {!isLoading && team && team.members.length === 0 && <p className="field-hint">{t('supervisor.emptyTeam')}</p>}

      <div className="team-list">
        {team?.members.map((member) => (
          <TeamRow key={member.id} member={member} isOwner={isOwner} onEdit={() => setEditingMember(member)} />
        ))}
      </div>

      <CreateUserModal
        isOpen={isCreateOpen} onClose={() => setIsCreateOpen(false)} isOwner={isOwner}
        allMembers={team?.members ?? []} ownerId={profile?.id ?? ''}
        onCreated={() => void loadTeam(search)}
      />

      {isOwner && editingMember && profile && (
        <EditUserModal
          isOpen={true}
          onClose={() => setEditingMember(null)}
          member={editingMember}
          allMembers={team?.members ?? []}
          ownerId={profile.id}
          onUpdated={() => void loadTeam(search)}
        />
      )}
    </div>
  );
}

function TeamRow({ member, isOwner, onEdit }: { member: TeamMemberDto; isOwner: boolean; onEdit: () => void }) {
  const { t } = useI18n();
  return (
    <div className="team-row">
      <span className={`team-row-status ${PRESENCE_CLASS[member.presenceStatus]}`} title={t(PRESENCE_LABEL_KEY[member.presenceStatus])} />
      <span className="team-row-name">
        {member.name}
        {!member.isActive && <span className="inactive-pill">{t('team.edit.statusInactive')}</span>}
      </span>
      <span className="team-row-role">{t(ROLE_LABEL_KEY[member.role] ?? 'supervisor.roleOperator')}</span>
      <span className="team-row-count">{member.quoteCount} cotações</span>
      {isOwner && (
        <button type="button" className="export-btn" onClick={onEdit}>
          {t('team.edit.editButton')}
        </button>
      )}
    </div>
  );
}

function CreateUserModal({
  isOpen, onClose, isOwner, allMembers, ownerId, onCreated,
}: {
  isOpen: boolean; onClose: () => void; isOwner: boolean; allMembers: TeamMemberDto[]; ownerId: string; onCreated: () => void;
}) {
  const { t } = useI18n();
  const [name, setName] = useState('');
  const [position, setPosition] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState<'OPERATOR' | 'SUPERVISOR'>('OPERATOR');
  const [supervisorId, setSupervisorId] = useState(ownerId);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const supervisorOptions = allMembers.filter((m) => m.role === 'SUPERVISOR');

  function resetAndClose() {
    setName(''); setPosition(''); setEmail(''); setPassword(''); setRole('OPERATOR'); setSupervisorId(ownerId); setError(null);
    onClose();
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setIsSubmitting(true);
    setError(null);

    try {
      const payload: CreateTeamUserRequest = {
        name, position: position || null, email, password, role,

        supervisorId: isOwner && role === 'OPERATOR' ? supervisorId : null,
      };
      await api.post('/api/team/users', payload);
      onCreated();
      resetAndClose();
    } catch (err) {
      setError(err instanceof ApiError && err.status === 409 ? t('supervisor.emailConflict') : t('supervisor.createError'));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Modal isOpen={isOpen} onClose={resetAndClose} title={t('supervisor.createUserTitle')}>
      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 18 }}>
        <div className="input-group">
          <label htmlFor="newUserName">{t('supervisor.nameLabel')}</label>
          <input id="newUserName" type="text" required value={name} onChange={(e) => setName(e.target.value)} />
        </div>
        <div className="input-group">
          <label htmlFor="newUserPosition">{t('supervisor.positionLabel')}</label>
          <input id="newUserPosition" type="text" value={position} onChange={(e) => setPosition(e.target.value)} />
        </div>
        <div className="input-group">
          <label htmlFor="newUserEmail">{t('supervisor.emailLabel')}</label>
          <input id="newUserEmail" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        </div>
        <div className="input-group">
          <label htmlFor="newUserPassword">{t('supervisor.passwordLabel')}</label>
          <input id="newUserPassword" type="password" required minLength={8} autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} />
        </div>

        {isOwner && (
          <div className="input-group">
            <label id="newUserRoleLabel">{t('supervisor.roleLabel')}</label>
            <div className="segmented" role="group" aria-labelledby="newUserRoleLabel">
              <button
                type="button" className={`segmented-option ${role === 'OPERATOR' ? 'is-active' : ''}`}
                onClick={() => setRole('OPERATOR')}
              >
                {t('supervisor.roleOperator')}
              </button>
              <button
                type="button" className={`segmented-option ${role === 'SUPERVISOR' ? 'is-active' : ''}`}
                onClick={() => setRole('SUPERVISOR')}
              >
                {t('supervisor.roleSupervisor')}
              </button>
            </div>
          </div>
        )}

        {isOwner && role === 'OPERATOR' && (
          <div className="input-group">
            <label id="newUserSupervisorLabel">{t('team.edit.supervisorLabel')}</label>
            <CustomSelect
              id="newUserSupervisor" aria-labelledby="newUserSupervisorLabel" value={supervisorId} onChange={setSupervisorId}
              options={[
                { value: ownerId, label: t('team.edit.supervisorOwnerOption') },
                ...supervisorOptions.map((s) => ({ value: s.id, label: s.name })),
              ]}
            />
          </div>
        )}

        {error && <div className="error-text">{error}</div>}

        <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
          <button type="button" className="btn-secondary" onClick={resetAndClose}>{t('supervisor.cancelButton')}</button>
          <button type="submit" className="btn-primary" style={{ marginBottom: 0 }} disabled={isSubmitting}>
            {isSubmitting ? t('supervisor.creating') : t('supervisor.createButton')}
          </button>
        </div>
      </form>
    </Modal>
  );
}
