import { useEffect } from 'react';
import { useAuth } from '../../contexts/AuthContext';
import { useI18n } from '../../contexts/I18nContext';

export function DemoBanner() {
  const { isDemo } = useAuth();
  const { t } = useI18n();

  useEffect(() => {
    document.body.classList.toggle('has-demo-banner', isDemo);
    return () => document.body.classList.remove('has-demo-banner');
  }, [isDemo]);

  if (!isDemo) return null;

  return <div className="demo-banner is-visible">{t('demoMode.banner')}</div>;
}
