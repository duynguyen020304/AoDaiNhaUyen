export interface CartItem {
  id: string;
  variantId: string;
  productId: string;
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
  id: string;
  userId: string;
  totalItemCount: number;
  subtotal: number;
  items: CartItem[];
}

export interface AddCartItemPayload {
  variantId: string;
  quantity: number;
}

export interface UpdateCartItemPayload {
  quantity: number;
}
