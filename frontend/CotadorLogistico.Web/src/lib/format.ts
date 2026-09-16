export function parseFlexibleDecimal(value: string): number {
  const cleaned = String(value).replace(',', '.').replace(/[^\d.]/g, '');
  return parseFloat(cleaned);
}

export function formatFlexibleDecimal(value: number, decimals: number): string {
  return value.toLocaleString('pt-BR', { minimumFractionDigits: decimals, maximumFractionDigits: decimals });
}

export function maskCep(value: string): string {
  const digits = value.replace(/\D/g, '').slice(0, 8);
  return digits.length > 5 ? `${digits.slice(0, 5)}-${digits.slice(5)}` : digits;
}

export function formatCepDisplay(cep: string): string {
  return cep.replace(/^(\d{5})(\d{3})$/, '$1-$2');
}

export function onlyDigits(value: string): string {
  return value.replace(/\D/g, '');
}

export function dateStamp(): string {
  const now = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}
