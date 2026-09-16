import { useI18n } from '../../contexts/I18nContext';
import type { QuoteResult } from '../../lib/quoting';

interface SummaryPanelProps {
  results: QuoteResult[];
}

export function SummaryPanel({ results }: SummaryPanelProps) {
  const { t } = useI18n();

  let valid = 0;
  let frenetPriceSum = 0;
  let mePriceSum = 0;
  let frenetTimeSum = 0;
  let meTimeSum = 0;

  for (const { selectedFrenet, selectedMe } of results) {
    if (selectedFrenet && selectedMe) {
      valid++;
      frenetPriceSum += selectedFrenet.priceBrl;
      mePriceSum += selectedMe.priceBrl;
      frenetTimeSum += selectedFrenet.deliveryDays;
      meTimeSum += selectedMe.deliveryDays;
    }
  }

  if (valid === 0) return null;

  const priceResult = describeWinner(frenetPriceSum / valid, mePriceSum / valid, t, 'summary.cheaperBy');
  const timeResult = describeWinner(frenetTimeSum / valid, meTimeSum / valid, t, 'summary.fasterBy');

  return (
    <div className="summary-container">
      <div className="summary-grid">
        <div className="summary-box">
          <div>{t('summary.avgPriceLabel')}</div>
          <div>
            <span className="summary-highlight">{priceResult.winnerLabel}</span>
            <span className="summary-desc">{priceResult.description}</span>
          </div>
        </div>
        <div className="summary-box">
          <div>{t('summary.avgTimeLabel')}</div>
          <div>
            <span className="summary-highlight">{timeResult.winnerLabel}</span>
            <span className="summary-desc">{timeResult.description}</span>
          </div>
        </div>
      </div>
    </div>
  );
}

function describeWinner(
  frenetAvg: number,
  meAvg: number,
  t: (key: string, vars?: Record<string, string | number>) => string,
  winKey: string,
) {
  if (frenetAvg < meAvg) {
    const diff = (((meAvg - frenetAvg) / meAvg) * 100).toFixed(1);
    return { winnerLabel: 'Frenet', description: t(winKey, { diff }) };
  }
  if (meAvg < frenetAvg) {
    const diff = (((frenetAvg - meAvg) / frenetAvg) * 100).toFixed(1);
    return { winnerLabel: 'Melhor Envio', description: t(winKey, { diff }) };
  }
  return { winnerLabel: t('summary.tie'), description: t('summary.tieDesc') };
}
