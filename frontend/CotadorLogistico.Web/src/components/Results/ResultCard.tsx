import { useRef, useState } from 'react';
import { useI18n } from '../../contexts/I18nContext';
import { useCurrency } from '../../contexts/CurrencyContext';
import { formatCepDisplay } from '../../lib/format';
import { compareWinners, type NormalizedOption, type QuoteResult } from '../../lib/quoting';
import { Popover } from '../common/Popover';
import { ChevronDownIcon } from '../common/Icons';

interface ResultCardProps {
  result: QuoteResult;
  comparisonMode: boolean;
  onSelectOption: (provider: 'FRENET' | 'MELHOR_ENVIO', option: NormalizedOption) => void;
}

export function ResultCard({ result, comparisonMode, onSelectOption }: ResultCardProps) {
  const { t } = useI18n();
  const { formatMoney } = useCurrency();
  const { destination, frenetOptions, meOptions, selectedFrenet, selectedMe } = result;

  const [openPopover, setOpenPopover] = useState<'frenet' | 'me' | 'compare' | null>(null);
  const frenetBtnRef = useRef<HTMLButtonElement>(null);
  const meBtnRef = useRef<HTMLButtonElement>(null);
  const compareBtnRef = useRef<HTMLButtonElement>(null);

  const { aPriceWins: frenetPriceWins, bPriceWins: mePriceWins, aTimeWins: frenetTimeWins, bTimeWins: meTimeWins } =
    compareWinners(selectedFrenet, selectedMe);

  if (!comparisonMode) {
    const cheapest = selectedFrenet;
    const others = frenetOptions?.slice(1) ?? [];

    return (
      <div className="result-card">
        <div className="card-header">
          <span>{destination.label}</span>
          <span>{formatCepDisplay(destination.cep)}</span>
        </div>

        {cheapest ? (
          <>
            <div className="service-row">
              <div className="service-top">
                <span className="service-price">{formatMoney(cheapest.priceBrl)}</span>
                <span className="service-name">{cheapest.carrier}</span>
              </div>
              <span className="service-time">{cheapest.deliveryDays} dias úteis</span>
            </div>

            {others.length > 0 && (
              <>
                <button ref={frenetBtnRef} type="button" className="expand-btn" onClick={() => setOpenPopover('frenet')}>
                  {t('results.expandOthers', { count: others.length })}
                  <ChevronDownIcon className="expand-icon" />
                </button>
                <Popover isOpen={openPopover === 'frenet'} onClose={() => setOpenPopover(null)} anchorEl={frenetBtnRef.current} title={t('results.moreOptions')}>
                  <OptionsList options={frenetOptions ?? []} formatMoney={formatMoney} />
                </Popover>
              </>
            )}
          </>
        ) : (
          <div className="error-text">{t('results.noRoute')}</div>
        )}
      </div>
    );
  }

  return (
    <div className="result-card">
      <div className="card-header">
        <span>{destination.label}</span>
        <span>{formatCepDisplay(destination.cep)}</span>
      </div>

      <div className="compare-grid">
        <ProviderColumn
          label="FRENET"
          option={selectedFrenet}
          priceWins={frenetPriceWins}
          timeWins={frenetTimeWins}
          unavailableLabel={t('results.unavailable')}
          formatMoney={formatMoney}
          moreCount={(frenetOptions?.length ?? 0) - 1}
          moreLabel={t('results.moreOptions')}
          btnRef={frenetBtnRef}
          onMoreClick={() => setOpenPopover('frenet')}
        />
        <ProviderColumn
          label="MELHOR ENVIO"
          option={selectedMe}
          priceWins={mePriceWins}
          timeWins={meTimeWins}
          unavailableLabel={t('results.unavailable')}
          formatMoney={formatMoney}
          moreCount={(meOptions?.length ?? 0) - 1}
          moreLabel={t('results.moreOptions')}
          btnRef={meBtnRef}
          onMoreClick={() => setOpenPopover('me')}
        />
      </div>

      {frenetOptions && meOptions && (
        <button ref={compareBtnRef} type="button" className="expand-btn" style={{ marginTop: 14 }} onClick={() => setOpenPopover('compare')}>
          {t('compare.compareButton')}
          <ChevronDownIcon className="expand-icon" />
        </button>
      )}

      <Popover isOpen={openPopover === 'frenet'} onClose={() => setOpenPopover(null)} anchorEl={frenetBtnRef.current} title="Frenet">
        <OptionsList
          options={frenetOptions ?? []}
          formatMoney={formatMoney}
          selectedCode={selectedFrenet}
          onSelect={(opt) => {
            onSelectOption('FRENET', opt);
            setOpenPopover(null);
          }}
          selectLabel={t('results.selectOption')}
        />
      </Popover>
      <Popover isOpen={openPopover === 'me'} onClose={() => setOpenPopover(null)} anchorEl={meBtnRef.current} title="Melhor Envio">
        <OptionsList
          options={meOptions ?? []}
          formatMoney={formatMoney}
          selectedCode={selectedMe}
          onSelect={(opt) => {
            onSelectOption('MELHOR_ENVIO', opt);
            setOpenPopover(null);
          }}
          selectLabel={t('results.selectOption')}
        />
      </Popover>
      <Popover isOpen={openPopover === 'compare'} onClose={() => setOpenPopover(null)} anchorEl={compareBtnRef.current} title={t('compare.title')} wide>
        <ComparePopoverContent
          frenetOptions={frenetOptions ?? []}
          meOptions={meOptions ?? []}
          selectedFrenet={selectedFrenet}
          selectedMe={selectedMe}
          onSelectFrenet={(opt) => onSelectOption('FRENET', opt)}
          onSelectMe={(opt) => onSelectOption('MELHOR_ENVIO', opt)}
        />
      </Popover>
    </div>
  );
}

function ProviderColumn({
  label, option, priceWins, timeWins, unavailableLabel, formatMoney, moreCount, moreLabel, btnRef, onMoreClick,
}: {
  label: string;
  option: NormalizedOption | null;
  priceWins: boolean;
  timeWins: boolean;
  unavailableLabel: string;
  formatMoney: (v: number) => string;
  moreCount: number;
  moreLabel: string;
  btnRef: React.RefObject<HTMLButtonElement | null>;
  onMoreClick: () => void;
}) {
  if (!option) {
    return (
      <div className="provider-col">
        <div className="provider-title">{label}</div>
        <div className="error-text">{unavailableLabel}</div>
      </div>
    );
  }

  return (
    <div className="provider-col">
      <div className="provider-title">{label} · {option.carrier}</div>
      <div className="metric-box">
        <span className={`metric-value ${priceWins ? 'win' : ''}`}>{formatMoney(option.priceBrl)}</span>
        <span className={`metric-sub ${timeWins ? 'win' : ''}`}>· {option.deliveryDays} dias</span>
      </div>
      {moreCount > 0 && (
        <button ref={btnRef} type="button" className="expand-btn" onClick={onMoreClick}>
          {moreLabel}
          <ChevronDownIcon className="expand-icon" />
        </button>
      )}
    </div>
  );
}

function OptionsList({
  options, formatMoney, selectedCode, onSelect, selectLabel,
}: {
  options: NormalizedOption[];
  formatMoney: (v: number) => string;
  selectedCode?: NormalizedOption | null;
  onSelect?: (option: NormalizedOption) => void;
  selectLabel?: string;
}) {
  const { t } = useI18n();
  return (
    <div>
      {options.map((option, index) => (
        <div className="popover-row" key={option.serviceCode ?? `${option.carrier}-${index}`}>
          <div>
            <div className="popover-row-main">{option.carrier}</div>
            <div className="popover-row-sub">{option.deliveryDays} {t('compare.timeLabel').toLowerCase()}</div>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <strong>{formatMoney(option.priceBrl)}</strong>
            {onSelect && (
              <button
                type="button"
                className={`popover-select-btn ${selectedCode === option ? 'is-selected' : ''}`}
                onClick={() => onSelect(option)}
              >
                {selectedCode === option ? t('results.selected') : selectLabel}
              </button>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}

function ComparePopoverContent({
  frenetOptions, meOptions, selectedFrenet, selectedMe, onSelectFrenet, onSelectMe,
}: {
  frenetOptions: NormalizedOption[];
  meOptions: NormalizedOption[];
  selectedFrenet: NormalizedOption | null;
  selectedMe: NormalizedOption | null;
  onSelectFrenet: (option: NormalizedOption) => void;
  onSelectMe: (option: NormalizedOption) => void;
}) {
  const { t } = useI18n();
  const { formatMoney } = useCurrency();
  const { aPriceWins, bPriceWins, aTimeWins, bTimeWins } = compareWinners(selectedFrenet, selectedMe);

  return (
    <div>
      <div className="compare-popover-columns">
        <div className="compare-popover-column">
          <div className="popover-title">FRENET</div>
          <OptionsList options={frenetOptions} formatMoney={formatMoney} selectedCode={selectedFrenet} onSelect={onSelectFrenet} selectLabel={t('results.selectOption')} />
        </div>
        <div className="compare-popover-column">
          <div className="popover-title">MELHOR ENVIO</div>
          <OptionsList options={meOptions} formatMoney={formatMoney} selectedCode={selectedMe} onSelect={onSelectMe} selectLabel={t('results.selectOption')} />
        </div>
      </div>

      {selectedFrenet && selectedMe && (
        <div style={{ marginTop: 16, paddingTop: 12, borderTop: '1px solid var(--line)', fontSize: 12 }}>
          <div>{t('compare.winnerPrice')}: <strong>{aPriceWins ? 'Frenet' : bPriceWins ? 'Melhor Envio' : t('summary.tie')}</strong></div>
          <div>{t('compare.winnerTime')}: <strong>{aTimeWins ? 'Frenet' : bTimeWins ? 'Melhor Envio' : t('summary.tie')}</strong></div>
        </div>
      )}
    </div>
  );
}
