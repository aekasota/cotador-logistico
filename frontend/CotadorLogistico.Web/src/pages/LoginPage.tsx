import { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useI18n } from '../contexts/I18nContext';
import { useAuth } from '../contexts/AuthContext';
import { BoxIcon } from '../components/common/Icons';

export function LoginPage() {
  const { t } = useI18n();
  const { status, signIn } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (status !== 'authenticated' && status !== 'demo') return;
    const redirectTo = (location.state as { from?: string } | null)?.from ?? '/';

    navigate(redirectTo, { replace: true, viewTransition: true });
  }, [status]);

  if (status === 'authenticated' || status === 'demo') return null;

  const isDemoTrigger = email.trim() === '--demomode';

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setIsSubmitting(true);
    setError(null);

    try {
      await signIn(email, password);
    } catch (err) {
      const message = err instanceof Error && 'status' in err ? t('auth.loginError') : t('auth.genericError');
      setError(message);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="login-shell">
      <div className="login-card page-enter">
        <div className="login-brand brand-transition-target">
          <BoxIcon />
          <span>{t('app.title')}</span>
        </div>

        <form className="login-form" onSubmit={handleSubmit}>
          <div className="input-group">
            <label htmlFor="loginEmail">{t('auth.emailLabel')}</label>
            <input
              id="loginEmail" type="text" autoComplete="username" required value={email}
              onChange={(e) => setEmail(e.target.value)} placeholder={t('auth.emailPlaceholder')}
            />
          </div>

          {!isDemoTrigger && (
            <div className="input-group">
              <label htmlFor="loginPassword">{t('auth.passwordLabel')}</label>
              <input
                id="loginPassword" type="password" autoComplete="current-password" required={!isDemoTrigger} value={password}
                onChange={(e) => setPassword(e.target.value)} placeholder={t('auth.passwordPlaceholder')}
              />
            </div>
          )}

          {error && <div className="login-error">{error}</div>}

          <button type="submit" className="btn-primary" style={{ justifyContent: 'center', marginBottom: 0 }} disabled={isSubmitting}>
            {isSubmitting ? t('auth.loggingIn') : t('auth.loginButton')}
          </button>
        </form>

        <p className="login-hint">{t('auth.demoHint')}</p>
      </div>
    </div>
  );
}
