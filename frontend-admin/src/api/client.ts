import type { ApiEnvelope, PaginatedApiEnvelope, ApiError } from '@/types/api'

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5043'

export class HttpError extends Error {
  readonly status: number
  readonly errors: ApiError[] | null
  readonly requestInfo?: { path: string; method: string }

  constructor(
    message: string,
    status: number,
    errors: ApiError[] | null = null,
    requestInfo?: { path: string; method: string },
  ) {
    super(message)
    this.name = 'HttpError'
    this.status = status
    this.errors = errors
    this.requestInfo = requestInfo
  }
}

function shouldSetJsonContentType(body: BodyInit | null | undefined): boolean {
  if (!body) return false
  return !(body instanceof FormData)
}

let refreshSessionPromise: Promise<boolean> | null = null

function shouldAttemptRefresh(path: string, response: Response): boolean {
  if (response.status !== 401) return false
  return ![
    '/api/auth/login',
    '/api/auth/logout',
    '/api/auth/refresh',
  ].includes(path)
}

async function refreshSession(): Promise<boolean> {
  if (!refreshSessionPromise) {
    refreshSessionPromise = fetch(`${API_BASE_URL}/api/auth/refresh`, {
      method: 'POST',
      credentials: 'include',
    })
      .then((response) => response.ok)
      .catch(() => false)
      .finally(() => {
        refreshSessionPromise = null
      })
  }

  return refreshSessionPromise
}

async function fetchWithCookies(path: string, init: RequestInit | undefined, headers: Headers): Promise<Response> {
  try {
    return await fetch(`${API_BASE_URL}${path}`, {
      credentials: 'include',
      ...init,
      headers,
    })
  } catch {
    throw new HttpError('Không thể kết nối đến máy chủ.', 0, null, {
      path,
      method: init?.method || 'GET',
    })
  }
}

async function fetchWithAuthRetry(path: string, init: RequestInit | undefined, headers: Headers): Promise<Response> {
  const response = await fetchWithCookies(path, init, headers)

  if (!shouldAttemptRefresh(path, response)) return response

  const refreshed = await refreshSession()
  if (!refreshed) return response

  return fetchWithCookies(path, init, headers)
}

export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers)
  if (shouldSetJsonContentType(init?.body) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetchWithAuthRetry(path, init, headers)

  if (response.status === 204) return undefined as T

  let payload: ApiEnvelope<T>
  try {
    payload = await response.json() as ApiEnvelope<T>
  } catch {
    throw new HttpError('Không thể đọc phản hồi từ máy chủ.', response.status, null, {
      path,
      method: init?.method || 'GET',
    })
  }

  if (!response.ok || !payload.success) {
    const message = payload.errors?.[0]?.message || payload.message || 'Đã xảy ra lỗi.'
    throw new HttpError(message, response.status, payload.errors, {
      path,
      method: init?.method || 'GET',
    })
  }

  return payload.data
}

export async function requestPaginated<T>(path: string, init?: RequestInit): Promise<PaginatedApiEnvelope<T>> {
  const headers = new Headers(init?.headers)
  if (shouldSetJsonContentType(init?.body) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetchWithAuthRetry(path, init, headers)

  let payload: PaginatedApiEnvelope<T>
  try {
    payload = await response.json() as PaginatedApiEnvelope<T>
  } catch {
    throw new HttpError('Không thể đọc phản hồi từ máy chủ.', response.status, null, {
      path,
      method: init?.method || 'GET',
    })
  }

  if (!response.ok || !payload.success) {
    const message = payload.errors?.[0]?.message || payload.message || 'Đã xảy ra lỗi.'
    throw new HttpError(message, response.status, payload.errors, {
      path,
      method: init?.method || 'GET',
    })
  }

  return payload
}
