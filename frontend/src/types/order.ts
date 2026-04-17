export interface OrderItem {
  id: number;
  productName: string;
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
  orderStatus: string;
  subtotal: number;
  discountAmount: number;
  shippingFee: number;
  totalAmount: number;
  placedAt: string;
  items: OrderItem[];
  paymentStatus: string | null;
}
