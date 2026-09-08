'use strict';

const URL_FRENET = '/api/frenet';
const URL_ME = '/api/melhorenvio';
const URL_SETTINGS = '/api/settings';

const URL_EXCHANGE_RATES = '/api/exchange-rates';

const capitais = [
    { cep: "69900060", label: "Rio Branco, AC" }, { cep: "57020000", label: "Maceió, AL" },
    { cep: "68900070", label: "Macapá, AP" }, { cep: "69005000", label: "Manaus, AM" },
    { cep: "45700000", label: "Salvador, BA" }, { cep: "60035000", label: "Fortaleza, CE" },
    { cep: "70040010", label: "Brasília, DF" }, { cep: "29010120", label: "Vitória, ES" },
    { cep: "74003010", label: "Goiânia, GO" }, { cep: "65010000", label: "São Luís, MA" },
    { cep: "78005000", label: "Cuiabá, MT" }, { cep: "79002000", label: "Campo Grande, MS" },
    { cep: "30110000", label: "Belo Horizonte, MG" }, { cep: "66015000", label: "Belém, PA" },
    { cep: "58010001", label: "João Pessoa, PB" }, { cep: "80010000", label: "Curitiba, PR" },
    { cep: "50010000", label: "Recife, PE" }, { cep: "64000010", label: "Teresina, PI" },
    { cep: "20010000", label: "Rio de Janeiro, RJ" }, { cep: "59293831", label: "Natal, RN" },
    { cep: "90010030", label: "Porto Alegre, RS" }, { cep: "76801000", label: "Porto Velho, RO" },
    { cep: "69301080", label: "Boa Vista, RR" }, { cep: "88010000", label: "Florianópolis, SC" },
    { cep: "01001000", label: "São Paulo, SP" }, { cep: "49010000", label: "Aracaju, SE" },
    { cep: "77001002", label: "Palmas, TO" }
];

let stats = { valid: 0, fPrice: 0, mPrice: 0, fTime: 0, mTime: 0 };

let settingsStatus = { frenetConfigured: false, melhorEnvioConfigured: false, theme: 'light', demoModeActive: false };

let selectedCurrency = localStorage.getItem('cotador-currency') || 'BRL';
let exchangeRates = { USD: null, MXN: null };


let lastQuotesData = [];
let lastBaseData = null;
let lastUsedCompareMode = false;

document.addEventListener('DOMContentLoaded', init);

async function init() {
    wireCalculatorEvents();
    wireSettingsModalEvents();
    wireHeaderEvents();
    wireNativeBridgeListener();

    await loadLanguage(detectInitialLanguage());
    applyCurrencySelectionUI();
    fetchExchangeRates();

    await refreshSettingsStatus();
}


async function refreshSettingsStatus() {
    try {
        const res = await fetch(URL_SETTINGS);
        settingsStatus = await res.json();
    } catch (e) {
        settingsStatus = { frenetConfigured: false, melhorEnvioConfigured: false, theme: 'light', demoModeActive: false };
    }
    applyTheme(settingsStatus.theme);
    applyMelhorEnvioLockState();
    applyFrenetGate();
    applyDemoModeBanner();
}

function applyTheme(theme) {
    const isDark = theme === 'dark';
    document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light');
}

function applyMelhorEnvioLockState() {
    const row = document.getElementById('meToggleRow');
    const toggle = document.getElementById('toggleME');
    const desc = document.getElementById('meToggleDesc');

    if (!settingsStatus.melhorEnvioConfigured) {
        row.classList.add('is-locked');
        toggle.checked = false;
        toggle.disabled = true;
        desc.textContent = t('options.compareLockedDesc');
    } else {
        row.classList.remove('is-locked');
        toggle.disabled = false;
        desc.textContent = t('options.compareDesc');
    }
}

function applyFrenetGate() {
    const btn = document.getElementById('btnCalcular');
    const notice = document.getElementById('frenetNotice');
    const isBusy = btn.dataset.busy === 'true';

    if (!settingsStatus.frenetConfigured) {
        btn.disabled = true;
        notice.classList.add('is-visible');
    } else {
        btn.disabled = isBusy;
        notice.classList.remove('is-visible');
    }
}

function applyDemoModeBanner() {
    document.getElementById('demoModeBanner').classList.toggle('is-visible', !!settingsStatus.demoModeActive);
}

function wireHeaderEvents() {
    document.getElementById('btnTheme').addEventListener('click', onToggleTheme);

    const regionPanel = document.getElementById('regionPanel');
    document.getElementById('btnRegion').addEventListener('click', (event) => {
        event.stopPropagation();
        regionPanel.classList.toggle('is-open');
    });
    document.addEventListener('click', (event) => {
        if (!regionPanel.contains(event.target)) regionPanel.classList.remove('is-open');
    });

    document.querySelectorAll('.region-option[data-lang]').forEach((btn) => {
        btn.addEventListener('click', async () => {
            await loadLanguage(btn.getAttribute('data-lang'));
            applyMelhorEnvioLockState();
            regionPanel.classList.remove('is-open');
        });
    });

    document.querySelectorAll('.region-option[data-currency]').forEach((btn) => {
        btn.addEventListener('click', () => {
            selectedCurrency = btn.getAttribute('data-currency');
            localStorage.setItem('cotador-currency', selectedCurrency);
            applyCurrencySelectionUI();
            reRenderAllResults();
            regionPanel.classList.remove('is-open');
        });
    });
}

async function onToggleTheme() {
    const newTheme = document.documentElement.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
    applyTheme(newTheme);
    settingsStatus.theme = newTheme;

    try {
        await fetch(URL_SETTINGS, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ frenetToken: null, melhorEnvioToken: null, theme: newTheme })
        });
    } catch (e) {

    }
}

function applyCurrencySelectionUI() {
    document.querySelectorAll('.region-option[data-currency]').forEach((btn) => {
        btn.classList.toggle('is-active', btn.getAttribute('data-currency') === selectedCurrency);
    });
}

async function fetchExchangeRates() {
    try {
        const res = await fetch(URL_EXCHANGE_RATES);
        const data = await res.json();
        exchangeRates.USD = parseFloat(data.USDBRL.bid);
        exchangeRates.MXN = parseFloat(data.MXNBRL.bid);
        setForeignCurrencyOptionsEnabled(true);
    } catch (e) {
        exchangeRates.USD = null;
        exchangeRates.MXN = null;
        setForeignCurrencyOptionsEnabled(false);
        if (selectedCurrency !== 'BRL') {
            selectedCurrency = 'BRL';
            localStorage.setItem('cotador-currency', 'BRL');
            applyCurrencySelectionUI();
        }
    }
}

function setForeignCurrencyOptionsEnabled(enabled) {
    document.querySelectorAll('.region-option[data-currency="USD"], .region-option[data-currency="MXN"]').forEach((btn) => {
        btn.disabled = !enabled;
        btn.style.opacity = enabled ? '1' : '0.4';
        btn.style.cursor = enabled ? 'pointer' : 'not-allowed';
    });
    document.getElementById('currencyUnavailableHint').style.display = enabled ? 'none' : 'block';
}

function convertFromBRL(valueInBRL) {
    if (selectedCurrency === 'BRL') return valueInBRL;
    const rate = exchangeRates[selectedCurrency];
    if (!rate) return valueInBRL;
    return valueInBRL / rate;
}

function formatMoney(v) {
    const brlValue = parseFloat(v);
    const converted = convertFromBRL(brlValue);
    const currency = exchangeRates[selectedCurrency] || selectedCurrency === 'BRL' ? selectedCurrency : 'BRL';
    const localeByCurrency = { BRL: 'pt-BR', USD: 'en-US', MXN: 'es-MX' };
    return converted.toLocaleString(localeByCurrency[currency] || 'pt-BR', { style: 'currency', currency });
}
function wireSettingsModalEvents() {
    const overlay = document.getElementById('settingsOverlay');

    document.getElementById('btnSettings').addEventListener('click', openSettingsModal);
    document.getElementById('btnCloseSettings').addEventListener('click', closeSettingsModal);
    document.getElementById('btnCancelSettings').addEventListener('click', closeSettingsModal);

    overlay.addEventListener('click', (event) => {
        if (event.target === overlay) closeSettingsModal();
    });
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape' && overlay.classList.contains('is-open')) closeSettingsModal();
    });

    document.getElementById('formSettings').addEventListener('submit', onSaveSettings);
    document.getElementById('btnExportLogs').addEventListener('click', () => {
        sendToNative({ type: 'export-logs' });
    });

    document.getElementById('btnToggleFrenetVisibility').addEventListener('click', () => {
        togglePasswordVisibility('inputFrenetToken', 'btnToggleFrenetVisibility');
    });
    document.getElementById('btnToggleMeVisibility').addEventListener('click', () => {
        togglePasswordVisibility('inputMeToken', 'btnToggleMeVisibility');
    });
}

function openSettingsModal() {
    const frenetInput = document.getElementById('inputFrenetToken');
    const meInput = document.getElementById('inputMeToken');

    frenetInput.value = '';
    meInput.value = '';
    frenetInput.placeholder = settingsStatus.frenetConfigured ? t('settings.frenetTokenPlaceholderSet') : t('settings.frenetTokenPlaceholder');
    meInput.placeholder = settingsStatus.melhorEnvioConfigured ? t('settings.meTokenPlaceholderSet') : t('settings.meTokenPlaceholder');

    const feedback = document.getElementById('settingsFeedback');
    feedback.className = 'settings-feedback';
    feedback.textContent = '';

    document.getElementById('settingsOverlay').classList.add('is-open');
}

function closeSettingsModal() {
    document.getElementById('settingsOverlay').classList.remove('is-open');
}

async function onSaveSettings(event) {
    event.preventDefault();
    const feedback = document.getElementById('settingsFeedback');
    const frenetToken = document.getElementById('inputFrenetToken').value.trim();
    const meToken = document.getElementById('inputMeToken').value.trim();

    try {
        const res = await fetch(URL_SETTINGS, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                frenetToken: frenetToken || null,
                melhorEnvioToken: meToken || null,
                theme: null
            })
        });

        if (!res.ok) throw new Error('resposta não-OK do servidor');

        settingsStatus = await res.json();
        applyMelhorEnvioLockState();
        applyFrenetGate();
        applyDemoModeBanner();

        feedback.textContent = t('settings.saveSuccess');
        feedback.className = 'settings-feedback is-success';
        setTimeout(closeSettingsModal, 1100);
    } catch (e) {
        feedback.textContent = t('settings.saveError');
        feedback.className = 'settings-feedback is-error';
    }
}

function togglePasswordVisibility(inputId, buttonId) {
    const input = document.getElementById(inputId);
    const button = document.getElementById(buttonId);
    const willShow = input.type === 'password';

    input.type = willShow ? 'text' : 'password';
    button.querySelector('.icon-eye').style.display = willShow ? 'none' : 'block';
    button.querySelector('.icon-eye-off').style.display = willShow ? 'block' : 'none';
}

function sendToNative(message) {
    if (!window.chrome || !window.chrome.webview) {
        showToast(t('export.errorMessage'), true);
        return;
    }
    window.chrome.webview.postMessage(message);
}

function wireNativeBridgeListener() {
    if (!window.chrome || !window.chrome.webview) return;
    window.chrome.webview.addEventListener('message', (event) => {
        const msg = event.data;
        if (!msg || msg.type !== 'export-result') return;
        if (msg.cancelled) return;

        showToast(msg.success ? t('export.successMessage') : (msg.error || t('export.errorMessage')), !msg.success);
    });
}

function showToast(message, isError) {
    const toast = document.getElementById('toast');
    toast.textContent = message;
    toast.className = 'toast is-visible ' + (isError ? 'is-error' : 'is-success');
    clearTimeout(showToast._timer);
    showToast._timer = setTimeout(() => toast.classList.remove('is-visible'), 3200);
}

function wireCalculatorEvents() {
    const toggleManual = document.getElementById('toggleManualCeps');
    const qtyInput = document.getElementById('cepQuantity');

    toggleManual.addEventListener('change', () => {
        document.getElementById('manual-ceps-container').style.display = toggleManual.checked ? 'block' : 'none';
        renderCepInputs();
    });
    qtyInput.addEventListener('input', renderCepInputs);

    document.getElementById('btnCalcular').addEventListener('click', onCalcularClick);
    document.getElementById('btnExportXlsx').addEventListener('click', exportXlsx);
    document.getElementById('btnExportChartCapital').addEventListener('click', () => exportChart('capital'));
    document.getElementById('btnExportChartSummary').addEventListener('click', () => exportChart('summary'));
}

function renderCepInputs() {
    const dynamicInputs = document.getElementById('dynamic-cep-inputs');
    const qtyInput = document.getElementById('cepQuantity');

    dynamicInputs.innerHTML = '';
    const qty = Math.min(parseInt(qtyInput.value) || 1, 50);
    for (let i = 0; i < qty; i++) {
        const div = document.createElement('div');
        div.className = 'input-group';
        div.innerHTML = `<input type="text" class="manual-cep" placeholder="CEP ${i + 1}" maxlength="8">`;
        dynamicInputs.appendChild(div);
    }
}

async function onCalcularClick() {
    const btn = document.getElementById('btnCalcular');
    const resultsArea = document.getElementById('resultsArea');
    const summaryArea = document.getElementById('summaryArea');
    const exportArea = document.getElementById('exportArea');
    const loading = document.getElementById('loadingIndicator');
    const useME = document.getElementById('toggleME').checked;

    const baseData = {
        sellerCep: document.getElementById('sellerCep').value.replace(/\D/g, ''),
        invoice: parseFloat(document.getElementById('invoiceValue').value),
        weight: parseFloat(document.getElementById('weight').value),
        length: parseFloat(document.getElementById('length').value),
        height: parseFloat(document.getElementById('height').value),
        width: parseFloat(document.getElementById('width').value)
    };

    if (!baseData.sellerCep) return alert('CEP de Origem obrigatório.');

    let destinos = [];
    if (document.getElementById('toggleManualCeps').checked) {
        document.querySelectorAll('.manual-cep').forEach((inp) => {
            const cep = inp.value.replace(/\D/g, '');
            if (cep.length === 8) destinos.push({ cep, label: "Destino Manual" });
        });
        if (destinos.length === 0) return alert('Preencha ao menos um CEP válido.');
    } else {
        destinos = capitais;
    }

    btn.dataset.busy = 'true';
    btn.disabled = true;
    summaryArea.style.display = 'none';
    exportArea.classList.remove('is-visible');
    loading.style.display = 'block';
    resultsArea.innerHTML = '';
    resultsArea.className = 'results-container ' + (useME ? 'grid-compare' : 'grid-single');
    stats = { valid: 0, fPrice: 0, mPrice: 0, fTime: 0, mTime: 0 };
    lastQuotesData = [];
    lastBaseData = baseData;
    lastUsedCompareMode = useME;

    let processed = 0;
    for (const destino of destinos) {
        loading.textContent = t('calculate.loadingProgress', { current: processed, total: destinos.length });

        if (useME) {
            const [resF, resM] = await Promise.all([fetchFrenet(baseData, destino), fetchME(baseData, destino)]);
            renderCompare(destino, resF, resM);
        } else {
            const resF = await fetchFrenet(baseData, destino);
            renderSingle(destino, resF);
        }

        processed++;
    }

    if (useME) renderSummary();
    loading.style.display = 'none';
    btn.dataset.busy = 'false';
    applyFrenetGate();

    if (lastQuotesData.length > 0) {
        exportArea.classList.add('is-visible');
        document.getElementById('btnExportChartCapital').style.display = useME ? 'inline-flex' : 'none';
        document.getElementById('btnExportChartSummary').style.display = useME ? 'inline-flex' : 'none';
    }
}

async function fetchFrenet(base, destino) {
    const payload = { "SellerCEP": base.sellerCep, "RecipientCEP": destino.cep, "ShipmentInvoiceValue": base.invoice, "ShippingItemArray": [{ "Weight": base.weight, "Length": base.length, "Height": base.height, "Width": base.width, "Quantity": 1, "isFragile": false }] };
    try {
        const res = await fetch(URL_FRENET, { method: 'POST', headers: { 'accept': 'application/json', 'content-type': 'application/json' }, body: JSON.stringify(payload) });
        if (!res.ok) return null;
        let data = await res.json();
        let valid = (data.ShippingSevicesArray || []).filter(s => !s.Error);
        valid.sort((a, b) => parseFloat(a.ShippingPrice) - parseFloat(b.ShippingPrice));
        return valid.length ? valid : null;
    } catch (e) { return null; }
}

async function fetchME(base, destino) {
    const payload = { "from": { "postal_code": base.sellerCep }, "to": { "postal_code": destino.cep }, "products": [{ "id": "1", "width": base.width, "height": base.height, "length": base.length, "weight": base.weight, "insurance_value": base.invoice, "quantity": 1 }] };
    try {
        const res = await fetch(URL_ME, { method: 'POST', headers: { 'Accept': 'application/json', 'Content-Type': 'application/json' }, body: JSON.stringify(payload) });
        if (!res.ok) return null;
        let data = await res.json();
        let valid = (data || []).filter(s => !s.error && s.custom_price);
        valid.sort((a, b) => parseFloat(a.custom_price) - parseFloat(b.custom_price));
        return valid.length ? valid : null;
    } catch (e) { return null; }
}

function scrollCardIntoView(card) {
    card.scrollIntoView({ behavior: 'smooth', block: 'end', inline: 'nearest' });
}

function renderSingle(destino, frenetData) {
    const card = document.createElement('div');
    card.className = 'result-card';
    const cepFmt = destino.cep.replace(/^(\d{5})(\d{3})$/, "$1-$2");
    let html = `<div class="card-header"><span>${destino.label}</span><span>${cepFmt}</span></div>`;

    const cheapest = frenetData && frenetData.length > 0
        ? { carrier: frenetData[0].Carrier, price: parseFloat(frenetData[0].ShippingPrice), time: parseInt(frenetData[0].DeliveryTime) }
        : null;

    lastQuotesData.push({ destino, frenet: cheapest, melhorEnvio: null });

    if (cheapest) {
        const others = frenetData.slice(1);

        html += `
            <div class="service-row">
                <div class="service-top">
                    <span class="service-name">${cheapest.carrier}</span>
                    <span class="service-price">${formatMoney(cheapest.price)}</span>
                </div>
                <span class="service-time">${cheapest.time} dias úteis</span>
            </div>
        `;

        if (others.length > 0) {
            let othersHtml = '<div class="other-services">';
            others.forEach(serv => {
                othersHtml += `<div class="other-service-row"><span>${serv.Carrier} (${serv.DeliveryTime}d)</span><span>${formatMoney(serv.ShippingPrice)}</span></div>`;
            });
            othersHtml += '</div>';

            html += `
                <div class="expand-btn" onclick="this.parentElement.classList.toggle('is-expanded')">
                    ${t('results.expandOthers', { count: others.length })}
                    <svg class="expand-icon" viewBox="0 0 24 24"><polyline points="6 9 12 15 18 9"></polyline></svg>
                </div>
                ${othersHtml}
            `;
        }
    } else {
        html += `<div class="error-text">${t('results.noRoute')}</div>`;
    }
    card.innerHTML = html;
    document.getElementById('resultsArea').appendChild(card);
    scrollCardIntoView(card);
}

function renderCompare(destino, fDataArr, mDataArr) {
    const card = document.createElement('div');
    card.className = 'result-card';
    const cepFmt = destino.cep.replace(/^(\d{5})(\d{3})$/, "$1-$2");
    let html = `<div class="card-header"><span>${destino.label}</span><span>${cepFmt}</span></div><div class="compare-grid">`;

    const fData = fDataArr ? { carrier: fDataArr[0].Carrier, price: parseFloat(fDataArr[0].ShippingPrice), time: parseInt(fDataArr[0].DeliveryTime) } : null;
    const mData = mDataArr ? { carrier: mDataArr[0].company.name, price: parseFloat(mDataArr[0].custom_price), time: parseInt(mDataArr[0].custom_delivery_time) } : null;

    lastQuotesData.push({ destino, frenet: fData, melhorEnvio: mData });

    html += `<div class="provider-col"><div class="provider-title">Frenet</div>`;
    if (!fData) html += `<div class="error-text">${t('results.unavailable')}</div>`;

    let meHtml = `<div class="provider-col"><div class="provider-title">Melhor Envio</div>`;
    if (!mData) meHtml += `<div class="error-text">${t('results.unavailable')}</div>`;

    if (fData && mData) {
        stats.valid++; stats.fPrice += fData.price; stats.mPrice += mData.price; stats.fTime += fData.time; stats.mTime += mData.time;
        const pClassF = fData.price < mData.price ? 'win' : (fData.price > mData.price ? 'lose' : 'tie');
        const pClassM = mData.price < fData.price ? 'win' : (mData.price > fData.price ? 'lose' : 'tie');
        const tClassF = fData.time < mData.time ? 'win' : (fData.time > mData.time ? 'lose' : 'tie');
        const tClassM = mData.time < fData.time ? 'win' : (mData.time > fData.time ? 'lose' : 'tie');

        html += `
            <div class="metric-box ${pClassF}"><span class="metric-label">Custo</span><span class="metric-value">${formatMoney(fData.price)}</span><span class="metric-sub">${fData.carrier}</span></div>
            <div class="metric-box ${tClassF}"><span class="metric-label">Prazo</span><span class="metric-value">${fData.time} dias</span></div>
        `;
        meHtml += `
            <div class="metric-box ${pClassM}"><span class="metric-label">Custo</span><span class="metric-value">${formatMoney(mData.price)}</span><span class="metric-sub">${mData.carrier}</span></div>
            <div class="metric-box ${tClassM}"><span class="metric-label">Prazo</span><span class="metric-value">${mData.time} dias</span></div>
        `;
    } else {
        if (fData) html += `<div class="metric-box tie"><span class="metric-label">Custo</span><span class="metric-value">${formatMoney(fData.price)}</span><span class="metric-sub">${fData.carrier}</span></div><div class="metric-box tie"><span class="metric-label">Prazo</span><span class="metric-value">${fData.time} dias</span></div>`;
        if (mData) meHtml += `<div class="metric-box tie"><span class="metric-label">Custo</span><span class="metric-value">${formatMoney(mData.price)}</span><span class="metric-sub">${mData.carrier}</span></div><div class="metric-box tie"><span class="metric-label">Prazo</span><span class="metric-value">${mData.time} dias</span></div>`;
    }

    html += `</div>${meHtml}</div></div>`;
    card.innerHTML = html;
    document.getElementById('resultsArea').appendChild(card);
    scrollCardIntoView(card);
}

function renderSummary() {
    if (stats.valid === 0) return;
    const aPF = stats.fPrice / stats.valid, aPM = stats.mPrice / stats.valid;
    const aTF = stats.fTime / stats.valid, aTM = stats.mTime / stats.valid;
    document.getElementById('summaryArea').style.display = 'block';

    const setRes = (winEl, descEl, val1, val2, name1, name2, cheaperKey) => {
        if (val1 < val2) {
            let diff = (((val2 - val1) / val2) * 100).toFixed(1);
            document.getElementById(winEl).innerText = name1;
            document.getElementById(descEl).innerText = t(cheaperKey, { diff });
        } else if (val2 < val1) {
            let diff = (((val1 - val2) / val1) * 100).toFixed(1);
            document.getElementById(winEl).innerText = name2;
            document.getElementById(descEl).innerText = t(cheaperKey, { diff });
        } else {
            document.getElementById(winEl).innerText = t('summary.tie');
            document.getElementById(descEl).innerText = t('summary.tieDesc');
        }
    };
    setRes('sumPriceWinner', 'sumPriceDesc', aPF, aPM, 'Frenet', 'Melhor Envio', 'summary.cheaperBy');
    setRes('sumTimeWinner', 'sumTimeDesc', aTF, aTM, 'Frenet', 'Melhor Envio', 'summary.fasterBy');
}

function reRenderAllResults() {
    if (lastQuotesData.length === 0) return;

    const resultsArea = document.getElementById('resultsArea');
    resultsArea.innerHTML = '';
    const entries = lastQuotesData;
    lastQuotesData = [];
    stats = { valid: 0, fPrice: 0, mPrice: 0, fTime: 0, mTime: 0 };

    entries.forEach(({ destino, frenet, melhorEnvio }) => {
        if (lastUsedCompareMode) {
            renderCompare(
                destino,
                frenet ? [{ Carrier: frenet.carrier, ShippingPrice: frenet.price, DeliveryTime: frenet.time }] : null,
                melhorEnvio ? [{ company: { name: melhorEnvio.carrier }, custom_price: melhorEnvio.price, custom_delivery_time: melhorEnvio.time }] : null
            );
        } else {
            renderSingle(destino, frenet ? [{ Carrier: frenet.carrier, ShippingPrice: frenet.price, DeliveryTime: frenet.time }] : null);
        }
    });

    if (lastUsedCompareMode) renderSummary();
}

function exportXlsx() {
    if (lastQuotesData.length === 0) return;

    const rows = lastQuotesData.map(({ destino, frenet, melhorEnvio }) => {
        const row = {
            [t('export.columnOriginCep')]: lastBaseData.sellerCep,
            [t('export.columnDestCep')]: destino.cep,
            [t('export.columnDestination')]: destino.label,
            [t('export.columnWeight')]: lastBaseData.weight,
            [t('export.columnLength')]: lastBaseData.length,
            [t('export.columnHeight')]: lastBaseData.height,
            [t('export.columnWidth')]: lastBaseData.width,
            [t('export.columnInvoiceValue')]: lastBaseData.invoice,
            [t('export.columnFrenetCarrier')]: frenet ? frenet.carrier : '',
            [t('export.columnFrenetPrice')]: frenet ? frenet.price : '',
            [t('export.columnFrenetTime')]: frenet ? frenet.time : ''
        };
        if (lastUsedCompareMode) {
            row[t('export.columnMeCarrier')] = melhorEnvio ? melhorEnvio.carrier : '';
            row[t('export.columnMePrice')] = melhorEnvio ? melhorEnvio.price : '';
            row[t('export.columnMeTime')] = melhorEnvio ? melhorEnvio.time : '';
        }
        return row;
    });

    const worksheet = XLSX.utils.json_to_sheet(rows);
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, t('export.sheetName'));
    const base64 = XLSX.write(workbook, { bookType: 'xlsx', type: 'base64' });

    sendToNative({
        type: 'export-file',
        kind: 'xlsx',
        fileName: `cotacoes-${dateStamp()}.xlsx`,
        base64Data: base64
    });
}

async function exportChart(kind) {
    if (lastQuotesData.length === 0) return;

    const container = document.createElement('div');
    container.style.cssText = 'position:fixed; left:-9999px; top:0;';
    const canvas = document.createElement('canvas');
    canvas.width = 900;
    canvas.height = 520;
    container.appendChild(canvas);
    document.body.appendChild(container);

    const chart = kind === 'capital' ? buildChartByCapital(canvas) : buildChartSummary(canvas);

    await new Promise((resolve) => setTimeout(resolve, 50));
    const dataUrl = canvas.toDataURL('image/png');

    chart.destroy();
    document.body.removeChild(container);

    const base64 = dataUrl.split(',')[1];
    const fileNameSuffix = kind === 'capital' ? 'por-capital' : 'resumo';
    sendToNative({
        type: 'export-file',
        kind: 'png',
        fileName: `grafico-${fileNameSuffix}-${dateStamp()}.png`,
        base64Data: base64
    });
}

function buildChartByCapital(canvas) {
    const labels = lastQuotesData.map(q => q.destino.label);
    const frenetPrices = lastQuotesData.map(q => q.frenet ? Number(convertFromBRL(q.frenet.price).toFixed(2)) : null);
    const mePrices = lastQuotesData.map(q => q.melhorEnvio ? Number(convertFromBRL(q.melhorEnvio.price).toFixed(2)) : null);

    return new Chart(canvas, {
        type: 'bar',
        data: {
            labels,
            datasets: [
                { label: t('export.chartFrenetLabel'), data: frenetPrices, backgroundColor: '#6366F1' },
                { label: t('export.chartMeLabel'), data: mePrices, backgroundColor: '#34D399' }
            ]
        },
        options: {
            responsive: false,
            animation: false,
            plugins: {
                title: { display: true, text: t('export.chartByCapitalTitle'), font: { size: 16 } },
                legend: { position: 'bottom' }
            },
            scales: { y: { beginAtZero: true } }
        }
    });
}

function buildChartSummary(canvas) {
    let frenetWins = 0, meWins = 0, ties = 0;
    lastQuotesData.forEach(({ frenet, melhorEnvio }) => {
        if (frenet && melhorEnvio) {
            if (frenet.price < melhorEnvio.price) frenetWins++;
            else if (melhorEnvio.price < frenet.price) meWins++;
            else ties++;
        }
    });

    return new Chart(canvas, {
        type: 'doughnut',
        data: {
            labels: [t('export.chartFrenetLabel'), t('export.chartMeLabel'), t('export.chartTieLabel')],
            datasets: [{ data: [frenetWins, meWins, ties], backgroundColor: ['#6366F1', '#34D399', '#94A3B8'] }]
        },
        options: {
            responsive: false,
            animation: false,
            plugins: {
                title: { display: true, text: t('export.chartSummaryTitle'), font: { size: 16 } },
                legend: { position: 'bottom' }
            }
        }
    });
}

function dateStamp() {
    const now = new Date();
    const pad = (n) => String(n).padStart(2, '0');
    return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}
