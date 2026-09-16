import { useI18n } from '../../contexts/I18nContext';
import { useToast } from '../common/Toast';
import { SpreadsheetIcon } from '../common/Icons';
import { exportQuotesXlsx } from '../../lib/exportXlsx';
import type { CargoBase, QuoteResult } from '../../lib/quoting';

interface ExportBarProps {
  results: QuoteResult[];
  base: CargoBase;
  comparisonMode: boolean;
}

export function ExportBar({ results, base, comparisonMode }: ExportBarProps) {
  const { t } = useI18n();
  const { showToast } = useToast();

  if (results.length === 0) return null;

  async function handleExport() {
    try {
      await exportQuotesXlsx(results, base, comparisonMode, t);
      showToast(t('export.successMessage'));
    } catch {
      showToast(t('export.errorMessage'), true);
    }
  }

  return (
    <div className="export-area">
      <button type="button" className="export-btn" onClick={() => void handleExport()}>
        <SpreadsheetIcon />
        <span>{t('export.spreadsheetButton')}</span>
      </button>
    </div>
  );
}
