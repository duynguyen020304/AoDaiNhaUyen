import { request } from './client';

const SESSION_KEY = 'aodainhauyen_session_id';

export type CustomerEventType =
  | 'viewed_product'
  | 'added_to_cart'
  | 'checkout_started'
  | 'checkout_completed'
  | 'promo_validated'
  | 'promo_applied'
  | 'ai_tryon_started'
  | 'ai_tryon_completed';

export interface TrackEventPayload {
  eventType: CustomerEventType;
  productId?: string | null;
  productVariantId?: string | null;
  orderId?: string | null;
  promoCodeId?: string | null;
  campaignId?: string | null;
  campaignSendId?: string | null;
  source?: string | null;
  medium?: string | null;
  campaign?: string | null;
  metadata?: Record<string, unknown>;
}

function createSessionId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return `session-${Date.now()}-${Math.random().toString(36).slice(2)}`;
}

export function getAnonymousSessionId(): string {
  if (typeof window === 'undefined') {
    return createSessionId();
  }

  try {
    const existing = window.localStorage.getItem(SESSION_KEY);
    if (existing) return existing;
    const next = createSessionId();
    window.localStorage.setItem(SESSION_KEY, next);
    return next;
  } catch {
    return createSessionId();
  }
}

export async function trackEvent(payload: TrackEventPayload): Promise<void> {
  try {
    if (typeof window === 'undefined') return;

    const params = new URLSearchParams(window.location.search);
    await request('/api/events', {
      method: 'POST',
      body: JSON.stringify({
        eventType: payload.eventType,
        anonymousSessionId: getAnonymousSessionId(),
        productId: payload.productId,
        productVariantId: payload.productVariantId,
        orderId: payload.orderId,
        promoCodeId: payload.promoCodeId,
        campaignId: payload.campaignId,
        campaignSendId: payload.campaignSendId,
        source: payload.source ?? params.get('utm_source'),
        medium: payload.medium ?? params.get('utm_medium'),
        campaign: payload.campaign ?? params.get('utm_campaign'),
        metadataJson: payload.metadata ? JSON.stringify(payload.metadata) : undefined,
      }),
    });
  } catch {
    // Analytics must never break UX.
  }
}
