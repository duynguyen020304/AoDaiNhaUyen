import type { PaginatedApiEnvelope } from './api';

export interface OrderItem {
  id: number;
  productName: string;
  sku: string | null;
  size: string | null;
  color: string | null;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  imageUrl: string | null;
}

export interface UserOrder {
  id: number;
  orderCode: string;
  recipientName: string;
  recipientPhone: string;
  province: string;
  district: string;
  ward: string | null;
  addressLine: string;
  orderStatus: string;
  subtotal: number;
  discountAmount: number;
  shippingFee: number;
  totalAmount: number;
  placedAt: string;
  items: OrderItem[];
  paymentStatus: string | null;
  note: string | null;
  confirmedAt: string | null;
  completedAt: string | null;
  cancelledAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export type PaginatedUserOrders = PaginatedApiEnvelope<UserOrder[]>;
