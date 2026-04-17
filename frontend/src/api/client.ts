import type { ApiEnvelope } from '../types/api';

const DEFAULT_API_BASE_URL = 'http://localhost:5043';

export const API_BASE_URL = (
  import.meta.env.VITE_API_BASE_URL ||
  import.meta.env.PUBLIC_BACKEND_DOMAIN ||
  DEFAULT_API_BASE_URL
).replace(/\/$/, '');

export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers);
  if (init?.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    credentials: 'include',
    ...init,
    headers,
  });
  const payload = await response.json() as ApiEnvelope<T>;

  if (!response.ok || !payload.success) {
    const message = payload.errors?.[0]?.message || payload.message || 'Không thể tải dữ liệu.';
    throw new Error(message);
  }

  return payload.data;
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
