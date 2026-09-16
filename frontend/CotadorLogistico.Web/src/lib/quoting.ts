import { api } from './apiClient';
import type { FrenetQuoteRaw, MelhorEnvioQuoteRaw, ShippingProvider } from '../types/api';

export interface CargoBase {
  sellerCep: string;
  invoice: number;
  weight: number;
  length: number;
  height: number;
  width: number;
}

export interface Destination {
  cep: string;
  label: string;
}

export interface NormalizedOption {
  provider: ShippingProvider;
  carrier: string;
  serviceName: string;
  serviceCode: string | null;
  priceBrl: number;
  deliveryDays: number;
}

function buildFrenetPayload(base: CargoBase, destinationCep: string) {
  return {
    SellerCEP: base.sellerCep,
    RecipientCEP: destinationCep,
    ShipmentInvoiceValue: base.invoice,
    ShippingItemArray: [
      { Weight: base.weight, Length: base.length, Height: base.height, Width: base.width, Quantity: 1, isFragile: false },
    ],
  };
}

function buildMelhorEnvioPayload(base: CargoBase, destinationCep: string) {
  return {
    from: { postal_code: base.sellerCep },
    to: { postal_code: destinationCep },
    products: [
      { id: '1', width: base.width, height: base.height, length: base.length, weight: base.weight, insurance_value: base.invoice, quantity: 1 },
    ],
  };
}

function parseFrenetResponse(raw: FrenetQuoteRaw): NormalizedOption[] {
  const valid = (raw.ShippingSevicesArray ?? []).filter((s) => !s.Error);
  valid.sort((a, b) => parseFloat(a.ShippingPrice) - parseFloat(b.ShippingPrice));
  return valid.map((s) => ({
    provider: 'FRENET',
    carrier: s.Carrier,
    serviceName: s.ServiceDescription ?? s.Carrier,
    serviceCode: s.ServiceCode ?? null,
    priceBrl: parseFloat(s.ShippingPrice),
    deliveryDays: parseInt(s.DeliveryTime, 10),
  }));
}

function parseMelhorEnvioResponse(raw: MelhorEnvioQuoteRaw): NormalizedOption[] {
  const valid = (raw ?? []).filter((s) => !s.error && s.custom_price);
  valid.sort((a, b) => parseFloat(a.custom_price) - parseFloat(b.custom_price));
  return valid.map((s) => ({
    provider: 'MELHOR_ENVIO',
    carrier: s.company.name,
    serviceName: s.name ?? s.company.name,
    serviceCode: s.id != null ? String(s.id) : null,
    priceBrl: parseFloat(s.custom_price),
    deliveryDays: Number(s.custom_delivery_time),
  }));
}

export async function fetchFrenet(base: CargoBase, destination: Destination, isDemo: boolean): Promise<NormalizedOption[] | null> {
  try {
    const payload = buildFrenetPayload(base, destination.cep);
    const path = isDemo ? '/api/demo/frenet' : '/api/shipping/frenet';
    const raw = await api.postRaw<FrenetQuoteRaw>(path, JSON.stringify(payload));
    const options = parseFrenetResponse(raw);
    return options.length ? options : null;
  } catch {
    return null;
  }
}

export async function fetchMelhorEnvio(base: CargoBase, destination: Destination, isDemo: boolean): Promise<NormalizedOption[] | null> {
  try {
    const payload = buildMelhorEnvioPayload(base, destination.cep);
    const path = isDemo ? '/api/demo/melhorenvio' : '/api/shipping/melhorenvio';
    const raw = await api.postRaw<MelhorEnvioQuoteRaw>(path, JSON.stringify(payload));
    const options = parseMelhorEnvioResponse(raw);
    return options.length ? options : null;
  } catch {
    return null;
  }
}

export interface QuoteResult {
  destination: Destination;
  frenetOptions: NormalizedOption[] | null;
  meOptions: NormalizedOption[] | null;

  selectedFrenet: NormalizedOption | null;
  selectedMe: NormalizedOption | null;
}

export function buildQuoteResult(
  destination: Destination,
  frenetOptions: NormalizedOption[] | null,
  meOptions: NormalizedOption[] | null,
): QuoteResult {
  return {
    destination,
    frenetOptions,
    meOptions,
    selectedFrenet: frenetOptions?.[0] ?? null,
    selectedMe: meOptions?.[0] ?? null,
  };
}

export function compareWinners(a: NormalizedOption | null, b: NormalizedOption | null) {
  if (!a || !b) return { aPriceWins: false, bPriceWins: false, aTimeWins: false, bTimeWins: false };
  return {
    aPriceWins: a.priceBrl < b.priceBrl,
    bPriceWins: b.priceBrl < a.priceBrl,
    aTimeWins: a.deliveryDays < b.deliveryDays,
    bTimeWins: b.deliveryDays < a.deliveryDays,
  };
}
