import { supabase } from './supabaseClient';

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5080';

let demoModeActive = false;
export function setDemoModeActive(active: boolean): void {
  demoModeActive = active;
}

export class ApiError extends Error {
  status: number;
  correlationId?: string;

  constructor(message: string, status: number, correlationId?: string) {
    super(message);
    this.status = status;
    this.correlationId = correlationId;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers);
  headers.set('Accept', 'application/json');
  if (init?.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json');

  if (!demoModeActive) {
    const { data } = await supabase.auth.getSession();
    const token = data.session?.access_token;
    if (token) headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(`${API_BASE_URL}${path}`, { ...init, headers });

  if (response.status === 204) return undefined as T;

  const contentType = response.headers.get('content-type') ?? '';
  const body = contentType.includes('application/json') ? await response.json() : await response.text();

  if (!response.ok) {
    const message = typeof body === 'object' && body && 'error' in body ? String(body.error) : 'Erro inesperado.';
    const correlationId = typeof body === 'object' && body && 'correlationId' in body ? String(body.correlationId) : undefined;

    if (message === 'ACCOUNT_DISABLED') {
      void supabase.auth.signOut().finally(() => {
        window.location.href = '/login';
      });
    }

    throw new ApiError(message, response.status, correlationId);
  }

  return body as T;
}

export const api = {
  get: <T>(path: string) => request<T>(path, { method: 'GET' }),
  post: <T>(path: string, body?: unknown) => request<T>(path, { method: 'POST', body: body === undefined ? undefined : JSON.stringify(body) }),
  patch: <T>(path: string, body?: unknown) => request<T>(path, { method: 'PATCH', body: body === undefined ? undefined : JSON.stringify(body) }),

  postRaw: <T>(path: string, rawJsonBody: string) => request<T>(path, { method: 'POST', body: rawJsonBody }),
};
