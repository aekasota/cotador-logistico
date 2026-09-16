import { useI18n } from '../contexts/I18nContext';
import { useAuth } from '../contexts/AuthContext';

const ROLE_KEY: Record<string, string> = { OPERATOR: 'profile.roleOperator', SUPERVISOR: 'profile.roleSupervisor', OWNER: 'profile.roleOwner' };

export function ProfilePage() {
  const { t } = useI18n();
  const { profile, isDemo } = useAuth();

  if (!profile) return null;

  return (
    <div className="profile-card page-enter">
      <div className="profile-name">
        {profile.name}
        {isDemo && <span className="demo-pill">DEMO</span>}
      </div>
      <div className="profile-position">{profile.position ?? '—'}</div>

      <div className="profile-row">
        <span className="profile-row-label">{t('profile.emailLabel')}</span>
        <span className="profile-row-value">{profile.email ?? '—'}</span>
      </div>
      <div className="profile-row">
        <span className="profile-row-label">{t('profile.roleLabel')}</span>
        <span className="profile-row-value">{t(ROLE_KEY[profile.role] ?? 'profile.roleOperator')}</span>
      </div>
      <div className="profile-row">
        <span className="profile-row-label">{t('profile.totalQuotesLabel')}</span>
        <span className="profile-row-value">{profile.totalQuotes}</span>
      </div>
      {profile.teamTotalQuotes !== null && (
        <div className="profile-row">
          <span className="profile-row-label">{t('profile.teamTotalLabel')}</span>
          <span className="profile-row-value">{profile.teamTotalQuotes}</span>
        </div>
      )}
    </div>
  );
}
