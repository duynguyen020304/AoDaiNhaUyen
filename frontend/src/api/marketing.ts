import { request } from './client';

export interface SubscribeResult {
  email: string;
  status: string;
  message: string;
}

export function subscribeToNewsletter(email: string): Promise<SubscribeResult> {
  return request<SubscribeResult>('/api/marketing/subscribe', {
    method: 'POST',
    body: JSON.stringify({ email, source: 'footer_newsletter' }),
  });
}

export function confirmNewsletter(token: string): Promise<SubscribeResult> {
  return request<SubscribeResult>('/api/marketing/confirm', {
    method: 'POST',
    body: JSON.stringify({ token }),
  });
}

export function unsubscribeFromNewsletter(token: string): Promise<SubscribeResult> {
  return request<SubscribeResult>('/api/marketing/unsubscribe', {
    method: 'POST',
    body: JSON.stringify({ token }),
  });
}
