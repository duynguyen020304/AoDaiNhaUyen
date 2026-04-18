export interface CartItem {
  id: number;
  variantId: number;
  productId: number;
  productName: string;
  productSlug: string;
  sku: string | null;
  variantName: string | null;
  size: string | null;
  color: string | null;
  imageUrl: string | null;
  price: number;
  salePrice: number | null;
  quantity: number;
  lineTotal: number;
}

export interface Cart {
  id: number;
  userId: number;
  totalItemCount: number;
  subtotal: number;
  items: CartItem[];
}

export interface AddCartItemPayload {
  variantId: number;
  quantity: number;
}

export interface UpdateCartItemPayload {
  quantity: number;
}
