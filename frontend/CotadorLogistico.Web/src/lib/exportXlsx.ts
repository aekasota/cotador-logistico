import { dateStamp } from './format';
import type { CargoBase, QuoteResult } from './quoting';

export async function exportQuotesXlsx(
  quotes: QuoteResult[],
  base: CargoBase,
  comparisonMode: boolean,
  t: (key: string) => string,
): Promise<void> {
  if (quotes.length === 0) return;

  const XLSX = await import('xlsx');

  const rows = quotes.map(({ destination, selectedFrenet, selectedMe }) => {
    const row: Record<string, string | number> = {
      [t('export.columnOriginCep')]: base.sellerCep,
      [t('export.columnDestCep')]: destination.cep,
      [t('export.columnDestination')]: destination.label,
      [t('export.columnWeight')]: base.weight,
      [t('export.columnLength')]: base.length,
      [t('export.columnHeight')]: base.height,
      [t('export.columnWidth')]: base.width,
      [t('export.columnInvoiceValue')]: base.invoice,
      [t('export.columnFrenetCarrier')]: selectedFrenet?.carrier ?? '',
      [t('export.columnFrenetPrice')]: selectedFrenet?.priceBrl ?? '',
      [t('export.columnFrenetTime')]: selectedFrenet?.deliveryDays ?? '',
    };

    if (comparisonMode) {
      row[t('export.columnMeCarrier')] = selectedMe?.carrier ?? '';
      row[t('export.columnMePrice')] = selectedMe?.priceBrl ?? '';
      row[t('export.columnMeTime')] = selectedMe?.deliveryDays ?? '';
    }

    return row;
  });

  const worksheet = XLSX.utils.json_to_sheet(rows);
  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, worksheet, t('export.sheetName'));
  XLSX.writeFile(workbook, `cotacoes-${dateStamp()}.xlsx`);
}
