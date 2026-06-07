import { request } from './client';

export interface CheckoutPayload {
  addressId?: string;
  address?: {
    recipientName: string;
    recipientPhone: string;
    province: string;
    district: string;
    ward?: string | null;
    addressLine: string;
  };
  note?: string;
  paymentMethod: string;
  promoCode?: string;
}

export interface CheckoutResult {
  orderId: string;
  orderCode: string;
  orderStatus: string;
  paymentStatus: string;
  subtotal: number;
  discountAmount: number;
  shippingFee: number;
  totalAmount: number;
  placedAt: string;
  appliedPromoCode: string | null;
  discountLabel: string | null;
}

export interface PromoValidationResult {
  isValid: boolean;
  errorCode: string | null;
  errorMessage: string | null;
  discountAmount: number;
  freeShipping: boolean;
  discountLabel: string | null;
}

export function checkout(payload: CheckoutPayload): Promise<CheckoutResult> {
  return request<CheckoutResult>('/api/users/me/checkout', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}

export function validatePromo(code: string, subtotal: number): Promise<PromoValidationResult> {
  return request<PromoValidationResult>('/api/promo/validate', {
    method: 'POST',
    body: JSON.stringify({ code, subtotal }),
  });
}
