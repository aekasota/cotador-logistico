import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { useTheme } from '../../contexts/ThemeContext';
import { useI18n, SUPPORTED_LANGUAGES, type Language } from '../../contexts/I18nContext';
import { useCurrency, type Currency } from '../../contexts/CurrencyContext';
import { useSettingsModal } from '../../contexts/SettingsModalContext';
import {
  BoxIcon, SunIcon, MoonIcon, GlobeIcon, GearIcon, UserIcon, UsersIcon, LogoutIcon,
} from '../common/Icons';

const LANGUAGE_FLAGS: Record<Language, string> = { 'pt-BR': '🇧🇷', 'es-MX': '🇲🇽', 'en-US': '🇬🇧' };
const LANGUAGE_NAMES: Record<Language, string> = { 'pt-BR': 'Português (BR)', 'es-MX': 'Español (MX)', 'en-US': 'English' };
const CURRENCY_FLAGS: Record<Currency, string> = { BRL: '🇧🇷', USD: '🇺🇸', MXN: '🇲🇽' };

export function Header() {
  const { t, language, setLanguage } = useI18n();
  const { toggleTheme } = useTheme();
  const { currency, setCurrency, ratesAvailable } = useCurrency();
  const { profile, isDemo, signOut } = useAuth();
  const { openSettings } = useSettingsModal();
  const navigate = useNavigate();

  const [isRegionOpen, setIsRegionOpen] = useState(false);
  const regionRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleOutsideClick(event: MouseEvent) {
      if (regionRef.current && !regionRef.current.contains(event.target as Node)) setIsRegionOpen(false);
    }
    document.addEventListener('click', handleOutsideClick);
    return () => document.removeEventListener('click', handleOutsideClick);
  }, []);

  const canSeeSupervisorArea = !isDemo && (profile?.role === 'SUPERVISOR' || profile?.role === 'OWNER');

  return (
    <div className="app-header">
      <h1 className="brand-transition-target">
        <Link to="/">
          <BoxIcon width={20} height={20} />
          <span>{t('app.title')}</span>
        </Link>
      </h1>

      <div className="header-actions">
        {!isDemo && (
          <button type="button" className="icon-btn" title={t('nav.profile')} aria-label={t('nav.profile')} onClick={() => navigate('/profile')}>
            <UserIcon />
          </button>
        )}

        {canSeeSupervisorArea && (
          <button type="button" className="icon-btn" title={t('nav.supervisor')} aria-label={t('nav.supervisor')} onClick={() => navigate('/supervisor')}>
            <UsersIcon />
          </button>
        )}

        <button type="button" className="icon-btn" title={t('theme.buttonLabel')} aria-label={t('theme.buttonLabel')} onClick={toggleTheme}>
          <SunIcon />
          <MoonIcon />
        </button>

        <div className="region-menu-wrapper" ref={regionRef}>
          <button
            type="button"
            className={`icon-btn ${isRegionOpen ? 'is-active' : ''}`}
            title={t('region.buttonLabel')}
            aria-label={t('region.buttonLabel')}
            onClick={() => setIsRegionOpen((open) => !open)}
          >
            <GlobeIcon />
          </button>
          <div className={`region-panel ${isRegionOpen ? 'is-open' : ''}`}>
            <div className="region-panel-label">{t('region.languageLabel')}</div>
            {SUPPORTED_LANGUAGES.map((lang) => (
              <button
                key={lang}
                type="button"
                className={`region-option ${language === lang ? 'is-active' : ''}`}
                onClick={() => {
                  setLanguage(lang);
                  setIsRegionOpen(false);
                }}
              >
                <span className="flag">{LANGUAGE_FLAGS[lang]}</span> {LANGUAGE_NAMES[lang]}
              </button>
            ))}

            <hr className="region-panel-divider" />

            <div className="region-panel-label">{t('region.currencyLabel')}</div>
            {(['BRL', 'USD', 'MXN'] as Currency[]).map((curr) => (
              <button
                key={curr}
                type="button"
                className={`region-option ${currency === curr ? 'is-active' : ''}`}
                disabled={curr !== 'BRL' && !ratesAvailable}
                style={curr !== 'BRL' && !ratesAvailable ? { opacity: 0.4, cursor: 'not-allowed' } : undefined}
                onClick={() => {
                  setCurrency(curr);
                  setIsRegionOpen(false);
                }}
              >
                <span className="flag">{CURRENCY_FLAGS[curr]}</span> {curr}
              </button>
            ))}
            {!ratesAvailable && <div className="field-hint" style={{ padding: '4px 8px 0' }}>{t('region.currencyUnavailable')}</div>}
          </div>
        </div>

        {!isDemo && (
          <button type="button" className="icon-btn" title={t('nav.settings')} aria-label={t('nav.settings')} onClick={openSettings}>
            <GearIcon />
          </button>
        )}

        <button type="button" className="icon-btn" title={t('nav.logout')} aria-label={t('nav.logout')} onClick={() => void signOut().then(() => navigate('/login'))}>
          <LogoutIcon />
        </button>
      </div>
    </div>
  );
}
