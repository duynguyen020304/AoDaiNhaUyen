import { request } from './client';

export interface CheckoutPayload {
  addressId?: number;
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
}

export interface CheckoutResult {
  orderId: number;
  orderCode: string;
  orderStatus: string;
  paymentStatus: string;
  subtotal: number;
  discountAmount: number;
  shippingFee: number;
  totalAmount: number;
  placedAt: string;
}

export function checkout(payload: CheckoutPayload): Promise<CheckoutResult> {
  return request<CheckoutResult>('/api/users/me/checkout', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}
