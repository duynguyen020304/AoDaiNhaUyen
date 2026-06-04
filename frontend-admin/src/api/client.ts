import type { ApiEnvelope, PaginatedApiEnvelope } from '@/types/api'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5043'

function shouldSetJsonContentType(body: BodyInit | null | undefined): boolean {
  if (!body) return false
  return !(body instanceof FormData)
}

export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers)
  if (shouldSetJsonContentType(init?.body) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  let response: Response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      credentials: 'include',
      ...init,
      headers,
    })
  } catch {
    throw new Error('Không thể kết nối đến máy chủ.')
  }

  let payload: ApiEnvelope<T>
  try {
    payload = await response.json() as ApiEnvelope<T>
  } catch {
    throw new Error('Không thể đọc phản hồi từ máy chủ.')
  }

  if (!response.ok || !payload.success) {
    const message = payload.errors?.[0]?.message || payload.message || 'Đã xảy ra lỗi.'
    throw new Error(message)
  }

  return payload.data
}

export async function requestPaginated<T>(path: string, init?: RequestInit): Promise<PaginatedApiEnvelope<T>> {
  const headers = new Headers(init?.headers)
  if (shouldSetJsonContentType(init?.body) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  let response: Response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      credentials: 'include',
      ...init,
      headers,
    })
  } catch {
    throw new Error('Không thể kết nối đến máy chủ.')
  }

  let payload: PaginatedApiEnvelope<T>
  try {
    payload = await response.json() as PaginatedApiEnvelope<T>
  } catch {
    throw new Error('Không thể đọc phản hồi từ máy chủ.')
  }

  if (!response.ok || !payload.success) {
    const message = payload.errors?.[0]?.message || payload.message || 'Đã xảy ra lỗi.'
    throw new Error(message)
  }

  return payload
}
