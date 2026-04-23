import { request, API_BASE_URL } from './client';

export interface ChatAttachment {
  id: number;
  kind: string;
  fileUrl: string;
  mimeType: string;
  originalFileName: string | null;
  createdAt: string;
}

export interface ChatRecommendationItem {
  productId: number;
  name: string;
  categorySlug: string;
  productType: string;
  price: number;
  salePrice: number | null;
  primaryImageUrl: string | null;
  primaryVariantId: number | null;
  rationale: string;
}

export interface ChatStructuredPayload {
  kind: string;
  scenario: string | null;
  canTryOn: boolean;
  requiresPersonImage: boolean;
  selectedGarmentProductId: number | null;
  selectedAccessoryProductIds: number[];
  pendingTryOnRequirements: string[];
  products: ChatRecommendationItem[];
  garmentProducts?: ChatRecommendationItem[] | null;
  accessoryProducts?: ChatRecommendationItem[] | null;
}

export interface ChatMessage {
  id: number;
  role: string;
  content: string;
  intent: string | null;
  createdAt: string;
  attachments: ChatAttachment[];
  structuredPayload: ChatStructuredPayload | null;
}

export interface ChatThreadSummary {
  id: number;
  title: string;
  preview: string | null;
  status: string;
  updatedAt: string;
}

export interface ChatThreadDetail {
  id: number;
  title: string;
  status: string;
  source: string;
  createdAt: string;
  updatedAt: string;
  messages: ChatMessage[];
}

export function listChatThreads(): Promise<ChatThreadSummary[]> {
  return request<ChatThreadSummary[]>('/api/v1/chat/threads');
}

export function createChatThread(): Promise<ChatThreadDetail> {
  return request<ChatThreadDetail>('/api/v1/chat/threads', { method: 'POST' });
}

export function getChatThread(threadId: number): Promise<ChatThreadDetail> {
  return request<ChatThreadDetail>(`/api/v1/chat/threads/${threadId}`);
}

export async function sendChatMessage(
  threadId: number,
  message: string,
  attachments: File[] = [],
): Promise<ChatThreadDetail> {
  const formData = new FormData();
  formData.append('message', message);
  attachments.forEach((attachment) => formData.append('attachments', attachment));

  return request<ChatThreadDetail>(`/api/v1/chat/threads/${threadId}/messages`, {
    method: 'POST',
    body: formData,
  });
}

export function executeChatTryOn(
  threadId: number,
  garmentProductId?: number | null,
  accessoryProductIds: number[] = [],
): Promise<ChatMessage> {
  return request<ChatMessage>(`/api/v1/chat/threads/${threadId}/try-on`, {
    method: 'POST',
    body: JSON.stringify({
      garmentProductId: garmentProductId ?? null,
      accessoryProductIds,
    }),
  });
}

export interface SseCreatedEvent {
  messageId: number;
  role: string;
  content: string;
  intent: string | null;
  createdAt: string;
  attachments: ChatAttachment[];
  structuredPayload: ChatStructuredPayload | null;
}

export interface SseTextDeltaEvent {
  delta: string;
}

export interface SseTextDoneEvent {
  fullText: string;
  messageId: number;
  createdAt: string;
}

export interface SseStreamErrorEvent {
  code: string;
  message: string;
}

export type SseChatEvent =
  | { type: 'created'; data: SseCreatedEvent }
  | { type: 'text_delta'; data: SseTextDeltaEvent }
  | { type: 'text_done'; data: SseTextDoneEvent }
  | { type: 'error'; data: SseStreamErrorEvent }
  | { type: 'done'; data: undefined };

export async function* streamChatMessage(
  threadId: number,
  message: string,
  attachments: File[] = [],
  signal?: AbortSignal,
): AsyncGenerator<SseChatEvent> {
  const formData = new FormData();
  formData.append('message', message);
  attachments.forEach((file) => formData.append('attachments', file));

  const response = await fetch(
    `${API_BASE_URL}/api/v1/chat/threads/${threadId}/messages/stream`,
    {
      method: 'POST',
      credentials: 'include',
      body: formData,
      signal,
    },
  );

  if (!response.ok) {
    let errorMessage = 'Không thể gửi tin nhắn.';
    try {
      const payload = await response.json();
      errorMessage = payload.errors?.[0]?.message || payload.message || errorMessage;
    } catch { /* ignore */ }
    yield { type: 'error', data: { code: 'request_failed', message: errorMessage } };
    return;
  }

  if (!response.body) {
    yield {
      type: 'error',
      data: { code: 'stream_unavailable', message: 'Không nhận được dữ liệu phản hồi trực tiếp.' },
    };
    return;
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  const parseEventBlock = (rawBlock: string): SseChatEvent | null => {
    if (!rawBlock.trim()) {
      return null;
    }

    let eventType = '';
    const dataLines: string[] = [];

    for (const rawLine of rawBlock.split('\n')) {
      const line = rawLine.trimEnd();
      if (!line || line.startsWith(':')) {
        continue;
      }

      if (line.startsWith('event:')) {
        eventType = line.slice(6).trim();
      } else if (line.startsWith('data:')) {
        dataLines.push(line.slice(5).trimStart());
      }
    }

    if (!eventType) {
      return null;
    }

    if (eventType === 'done') {
      return { type: 'done', data: undefined };
    }

    try {
      const rawData = dataLines.join('\n');
      switch (eventType) {
        case 'created':
          return { type: 'created', data: JSON.parse(rawData) as SseCreatedEvent };
        case 'text_delta':
          return { type: 'text_delta', data: JSON.parse(rawData) as SseTextDeltaEvent };
        case 'text_done':
          return { type: 'text_done', data: JSON.parse(rawData) as SseTextDoneEvent };
        case 'error':
          return { type: 'error', data: JSON.parse(rawData) as SseStreamErrorEvent };
        default:
          return null;
      }
    } catch {
      return {
        type: 'error',
        data: { code: 'malformed_stream_event', message: 'Dữ liệu phản hồi trực tiếp không hợp lệ.' },
      };
    }
  };

  while (true) {
    const { done, value } = await reader.read();
    buffer += decoder.decode(value ?? new Uint8Array(), { stream: !done }).replace(/\r\n?/g, '\n');

    let boundaryIndex = buffer.indexOf('\n\n');
    while (boundaryIndex >= 0) {
      const eventBlock = buffer.slice(0, boundaryIndex);
      buffer = buffer.slice(boundaryIndex + 2);
      const parsedEvent = parseEventBlock(eventBlock);
      if (parsedEvent) {
        yield parsedEvent;
      }

      boundaryIndex = buffer.indexOf('\n\n');
    }

    if (done) {
      break;
    }
  }

  const finalEvent = parseEventBlock(buffer);
  if (finalEvent) {
    yield finalEvent;
  }
}
