export type Role = 'OPERATOR' | 'SUPERVISOR' | 'OWNER';

export type PresenceStatus = 'ONLINE' | 'QUOTING' | 'OFFLINE';

export type ShippingProvider = 'FRENET' | 'MELHOR_ENVIO';

export interface ProfileResponse {
  id: string;
  name: string;
  position: string | null;
  email: string | null;
  role: Role;
  theme: 'light' | 'dark';
  language: string;
  totalQuotes: number;
  teamTotalQuotes: number | null;
  mustChangePassword: boolean;
}

export interface IntegrationAuditDto {
  updatedAt: string;
  updatedByName: string | null;
}

export interface SettingsStatusResponse {
  frenetConfigured: boolean;
  frenetAudit: IntegrationAuditDto | null;
  melhorEnvioConfigured: boolean;
  melhorEnvioAudit: IntegrationAuditDto | null;
  geminiConfigured: boolean;
  geminiAudit: IntegrationAuditDto | null;
}

export interface UpdateSettingsRequest {
  frenetToken: string | null;
  melhorEnvioToken: string | null;
  geminiApiKey: string | null;
}

export interface ExchangeRateSnapshot {
  currency: 'USD' | 'MXN';
  rateToBrl: number;
  rateFromBrl: number;
  source: string;
  effectiveDate: string;
  updatedAt: string;
}

export interface TeamMemberDto {
  id: string;
  name: string;

  email: string | null;
  position: string | null;
  role: Role;
  supervisorId: string | null;
  isActive: boolean;
  presenceStatus: PresenceStatus;
  quoteCount: number;
}

export interface TeamResponse {
  members: TeamMemberDto[];
  totalMembers: number;
  totalQuotes: number;
}

export interface UpdateUserAdminRequest {
  name?: string | null;
  position?: string | null;
  email?: string | null;
  supervisorId?: string | null;
  role?: 'OPERATOR' | 'SUPERVISOR' | null;
}

export interface AdminUserDto {
  id: string;
  name: string;
  email: string | null;
  position: string | null;
  role: Role;
  supervisorId: string | null;
  isActive: boolean;
}

export interface SetActiveRequest {
  isActive: boolean;
}

export interface TemporaryPasswordResponse {
  temporaryPassword: string;
}

export interface ChangePasswordRequest {
  newPassword: string;
}

export interface CreateTeamUserRequest {
  name: string;
  position: string | null;
  email: string;
  password: string;
  role: 'OPERATOR' | 'SUPERVISOR' | null;
  supervisorId: string | null;
}

export interface CreateTeamUserResponse {
  id: string;
  name: string;
  email: string;
  role: Role;
}

export interface PersistQuoteOptionRequest {
  provider: ShippingProvider;
  carrier: string;
  serviceName: string;
  serviceCode: string | null;
  priceBrl: number;
  deliveryDays: number;
  isWinnerPrice: boolean;
  isWinnerTime: boolean;
  wasSelectedInComparison: boolean;
}

export interface PersistQuoteRequest {
  sourceCep: string;
  destinationCep: string;
  destinationLabel: string | null;
  packageWeightKg: number;
  packageLengthCm: number;
  packageWidthCm: number;
  packageHeightCm: number;
  packageQuantity: number;
  declaredValueBrl: number;
  comparisonMode: boolean;
  currency: 'BRL' | 'USD' | 'MXN';
  exchangeRateUsed: number | null;
  options: PersistQuoteOptionRequest[];
}

export interface CapitalAdvantage {
  destinationLabel: string;
  frenetAvgPriceBrl: number;
  melhorEnvioAvgPriceBrl: number;
  advantagePercent: number;
  sampleSize: number;
}

export interface PackageDimensionSuggestion {
  lengthCm: number;
  widthCm: number;
  heightCm: number;
  confidence: 'low' | 'medium' | 'high';
  reason: string;
}

export interface PackageDimensionsResponse {
  allowed: boolean;
  suggestions: PackageDimensionSuggestion[];
  warning: string;
}

export interface FrenetServiceRaw {
  Carrier: string;
  ServiceDescription?: string;
  ShippingPrice: string;
  DeliveryTime: string;
  Error?: boolean;
  ServiceCode?: string;
}

export interface FrenetQuoteRaw {
  ShippingSevicesArray?: FrenetServiceRaw[];
}

export interface MelhorEnvioServiceRaw {
  id?: string | number;
  name?: string;
  company: { name: string; id?: number };
  custom_price: string;
  custom_delivery_time: number;
  error?: string;
}

export type MelhorEnvioQuoteRaw = MelhorEnvioServiceRaw[];
