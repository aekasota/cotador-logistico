import type { Currency } from '../contexts/CurrencyContext';
import type { PersistQuoteOptionRequest, PersistQuoteRequest } from '../types/api';
import type { NormalizedOption, QuoteResult } from './quoting';
import type { CargoFormState } from '../components/QuoteForm/QuoteForm';
import { parseFlexibleDecimal } from './format';

export function buildPersistQuoteRequest(
  result: QuoteResult,
  form: CargoFormState,
  comparisonMode: boolean,
  currency: Currency,
  exchangeRateUsed: number | null,
): PersistQuoteRequest {
  const allOptions = [...(result.frenetOptions ?? []), ...(result.meOptions ?? [])];
  const cheapestPrice = allOptions.length ? Math.min(...allOptions.map((o) => o.priceBrl)) : null;
  const fastestTime = allOptions.length ? Math.min(...allOptions.map((o) => o.deliveryDays)) : null;

  const toOptionRequest = (option: NormalizedOption, wasSelected: boolean): PersistQuoteOptionRequest => ({
    provider: option.provider,
    carrier: option.carrier,
    serviceName: option.serviceName,
    serviceCode: option.serviceCode,
    priceBrl: option.priceBrl,
    deliveryDays: option.deliveryDays,
    isWinnerPrice: cheapestPrice !== null && option.priceBrl === cheapestPrice,
    isWinnerTime: fastestTime !== null && option.deliveryDays === fastestTime,
    wasSelectedInComparison: wasSelected,
  });

  const options = allOptions.map((option) =>
    toOptionRequest(option, option === result.selectedFrenet || option === result.selectedMe),
  );

  return {
    sourceCep: form.sellerCep.replace(/\D/g, ''),
    destinationCep: result.destination.cep,
    destinationLabel: result.destination.label,
    packageWeightKg: parseFlexibleDecimal(form.weight),
    packageLengthCm: parseFlexibleDecimal(form.length),
    packageWidthCm: parseFlexibleDecimal(form.width),
    packageHeightCm: parseFlexibleDecimal(form.height),
    packageQuantity: 1,
    declaredValueBrl: parseFlexibleDecimal(form.invoiceValue),
    comparisonMode,
    currency,
    exchangeRateUsed,
    options,
  };
}
