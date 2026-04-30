import { request } from './client';
import type { AddCartItemPayload, Cart, UpdateCartItemPayload } from '../types/cart';

export const CART_UPDATED_EVENT = 'aodai:cart-updated';

export function getCart(): Promise<Cart> {
  return request<Cart>('/api/users/me/cart');
}

export async function addCartItem(payload: AddCartItemPayload): Promise<Cart> {
  const cart = await request<Cart>('/api/users/me/cart/items', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
  dispatchCartUpdated(cart);
  return cart;
}

export async function updateCartItem(itemId: number, payload: UpdateCartItemPayload): Promise<Cart> {
  const cart = await request<Cart>(`/api/users/me/cart/items/${itemId}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  });
  dispatchCartUpdated(cart);
  return cart;
}

export async function removeCartItem(itemId: number): Promise<Cart> {
  const cart = await request<Cart>(`/api/users/me/cart/items/${itemId}`, {
    method: 'DELETE',
  });
  dispatchCartUpdated(cart);
  return cart;
}

export async function clearCart(): Promise<boolean> {
  const result = await request<boolean>('/api/users/me/cart', {
    method: 'DELETE',
  });
  dispatchCartUpdated({ totalItemCount: 0 });
  return result;
}

function dispatchCartUpdated(cart: Pick<Cart, 'totalItemCount'>): void {
  window.dispatchEvent(new CustomEvent(CART_UPDATED_EVENT, { detail: cart.totalItemCount }));
}
