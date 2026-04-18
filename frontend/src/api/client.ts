import type { ApiEnvelope, PaginatedApiEnvelope } from '../types/api';

const DEFAULT_API_BASE_URL = 'http://localhost:5043';

function isLocalhostUrl(value: string): boolean {
  try {
    const url = new URL(value);
    return url.hostname === 'localhost' || url.hostname === '127.0.0.1';
  } catch {
    return false;
  }
}

function resolveRegionalApiBaseUrl(): string {
  const configuredBaseUrl =
    import.meta.env.VITE_API_BASE_URL ||
    import.meta.env.PUBLIC_BACKEND_DOMAIN;

  if (typeof window === 'undefined') {
    return configuredBaseUrl || DEFAULT_API_BASE_URL;
  }

  let regionalBaseUrl: string | null = null;
  switch (window.location.hostname) {
    case 'aodainhauyen.io.vn':
      regionalBaseUrl = 'https://api-hk1.aodainhauyen.io.vn';
      break;
    case 'backup.aodainhauyen.io.vn':
      regionalBaseUrl = 'https://api-us1.aodainhauyen.io.vn';
      break;
    default:
      regionalBaseUrl = null;
      break;
  }

  if (regionalBaseUrl && (!configuredBaseUrl || isLocalhostUrl(configuredBaseUrl))) {
    return regionalBaseUrl;
  }

  return configuredBaseUrl || DEFAULT_API_BASE_URL;
}

export const API_BASE_URL = resolveRegionalApiBaseUrl().replace(/\/$/, '');

function shouldSetJsonContentType(body: BodyInit | null | undefined): boolean {
  if (!body) {
    return false;
  }

  return !(body instanceof FormData);
}

export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers);
  if (shouldSetJsonContentType(init?.body) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      credentials: 'include',
      ...init,
      headers,
    });
  } catch {
    throw new Error('Không thể kết nối đến máy chủ. Vui lòng thử lại sau.');
  }

  let payload: ApiEnvelope<T>;
  try {
    payload = await response.json() as ApiEnvelope<T>;
  } catch {
    throw new Error('Không thể đọc phản hồi từ máy chủ. Vui lòng thử lại sau.');
  }

  if (!response.ok || !payload.success) {
    const message = payload.errors?.[0]?.message || payload.message || 'Không thể tải dữ liệu.';
    throw new Error(message);
  }

  return payload.data;
}

export async function requestPaginated<T>(path: string, init?: RequestInit): Promise<PaginatedApiEnvelope<T>> {
  const headers = new Headers(init?.headers);
  if (shouldSetJsonContentType(init?.body) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      credentials: 'include',
      ...init,
      headers,
    });
  } catch {
    throw new Error('Không thể kết nối đến máy chủ. Vui lòng thử lại sau.');
  }

  let payload: PaginatedApiEnvelope<T>;
  try {
    payload = await response.json() as PaginatedApiEnvelope<T>;
  } catch {
    throw new Error('Không thể đọc phản hồi từ máy chủ. Vui lòng thử lại sau.');
  }

  if (!response.ok || !payload.success) {
    const message = payload.errors?.[0]?.message || payload.message || 'Không thể tải dữ liệu.';
    throw new Error(message);
  }

  return payload;
}

export function resolveAssetUrl(url: string | null): string | null {
  if (!url) {
    return null;
  }

  if (/^https?:\/\//i.test(url)) {
    return url;
  }

  return `${API_BASE_URL}${url.startsWith('/') ? url : `/${url}`}`;
}
