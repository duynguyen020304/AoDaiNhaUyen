import { request } from './client';
import { queryClient } from '../lib/queryClient';
import { queryKeys } from '../lib/queryKeys';
import { trackEvent } from './events';
import type { AddCartItemPayload, Cart, UpdateCartItemPayload } from '../types/cart';
import { emptyCartFrom, normalizeCartAssets } from '../utils/cartMapping';

export function getCart(): Promise<Cart> {
  return request<Cart>('/api/users/me/cart');
}

export async function addCartItem(payload: AddCartItemPayload): Promise<Cart> {
  const cart = await request<Cart>('/api/users/me/cart/items', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
  updateCartCache(cart);
  void trackEvent({ eventType: 'added_to_cart', productVariantId: payload.variantId, metadata: { quantity: payload.quantity } });
  return cart;
}

export async function updateCartItem(itemId: string, payload: UpdateCartItemPayload): Promise<Cart> {
  const cart = await request<Cart>(`/api/users/me/cart/items/${itemId}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  });
  updateCartCache(cart);
  return cart;
}

export async function removeCartItem(itemId: string): Promise<Cart> {
  const cart = await request<Cart>(`/api/users/me/cart/items/${itemId}`, {
    method: 'DELETE',
  });
  updateCartCache(cart);
  return cart;
}

export async function clearCart(): Promise<boolean> {
  const result = await request<boolean>('/api/users/me/cart', {
    method: 'DELETE',
  });
  queryClient.setQueryData<Cart | undefined>(queryKeys.cart.current, emptyCartFrom);
  void queryClient.invalidateQueries({ queryKey: queryKeys.cart.current });
  return result;
}

function updateCartCache(cart: Cart): void {
  queryClient.setQueryData(queryKeys.cart.current, normalizeCartAssets(cart));
  void queryClient.invalidateQueries({ queryKey: queryKeys.cart.current });
}
