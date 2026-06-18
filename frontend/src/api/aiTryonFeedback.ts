import { getGuestKey } from './guestKey';
import { request } from './client';

export interface CreateAiTryOnFeedbackPayload {
  generatedImageId: string;
  rating: number;
  comment?: string;
}

export interface AiTryOnFeedback {
  id: string;
  generatedImageId: string;
  rating: number;
  comment: string | null;
  createdAt: string;
}

export function createAiTryOnFeedback(payload: CreateAiTryOnFeedbackPayload): Promise<AiTryOnFeedback> {
  return request<AiTryOnFeedback>('/api/v1/ai-tryon/feedback', {
    method: 'POST',
    headers: { 'X-Guest-Key': getGuestKey() },
    body: JSON.stringify(payload),
  });
}
