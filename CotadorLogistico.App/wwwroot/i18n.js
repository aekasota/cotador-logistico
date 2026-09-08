'use strict';
const SUPPORTED_LANGUAGES = ['pt-BR', 'es-MX', 'en-US'];
const DEFAULT_LANGUAGE = 'pt-BR';

let currentTranslations = {};
let currentLanguage = DEFAULT_LANGUAGE;

function detectInitialLanguage() {
    const stored = localStorage.getItem('cotador-language');
    if (stored && SUPPORTED_LANGUAGES.includes(stored)) return stored;

    const browserLang = (navigator.language || DEFAULT_LANGUAGE).toLowerCase();
    if (browserLang.startsWith('es')) return 'es-MX';
    if (browserLang.startsWith('en')) return 'en-US';
    return DEFAULT_LANGUAGE;
}

async function loadLanguage(lang) {
    if (!SUPPORTED_LANGUAGES.includes(lang)) lang = DEFAULT_LANGUAGE;

    try {
        const response = await fetch(`i18n/${lang}.json`);
        currentTranslations = await response.json();
        currentLanguage = lang;
    } catch (error) {
        currentTranslations = {};
        currentLanguage = DEFAULT_LANGUAGE;
    }

    localStorage.setItem('cotador-language', currentLanguage);
    document.documentElement.lang = currentLanguage;
    applyTranslations();
}

function t(keyPath, vars) {
    const parts = keyPath.split('.');
    let node = currentTranslations;
    for (const part of parts) {
        if (node == null) return keyPath;
        node = node[part];
    }
    if (typeof node !== 'string') return keyPath;

    if (vars) {
        return Object.keys(vars).reduce(
            (text, key) => text.split(`{${key}}`).join(String(vars[key])),
            node
        );
    }
    return node;
}

function applyTranslations() {
    document.querySelectorAll('[data-i18n]').forEach((el) => {
        el.textContent = t(el.getAttribute('data-i18n'));
    });
    document.querySelectorAll('[data-i18n-placeholder]').forEach((el) => {
        el.placeholder = t(el.getAttribute('data-i18n-placeholder'));
    });
    document.querySelectorAll('[data-i18n-title]').forEach((el) => {
        el.title = t(el.getAttribute('data-i18n-title'));
    });

    document.querySelectorAll('.region-option[data-lang]').forEach((btn) => {
        btn.classList.toggle('is-active', btn.getAttribute('data-lang') === currentLanguage);
    });
}
