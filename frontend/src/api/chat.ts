import { request } from './client';

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
