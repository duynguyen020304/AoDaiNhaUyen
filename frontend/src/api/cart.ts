import { request } from './client';
import type { AddCartItemPayload, Cart, UpdateCartItemPayload } from '../types/cart';

export function getCart(): Promise<Cart> {
  return request<Cart>('/api/users/me/cart');
}

export function addCartItem(payload: AddCartItemPayload): Promise<Cart> {
  return request<Cart>('/api/users/me/cart/items', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}

export function updateCartItem(itemId: number, payload: UpdateCartItemPayload): Promise<Cart> {
  return request<Cart>(`/api/users/me/cart/items/${itemId}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  });
}

export function removeCartItem(itemId: number): Promise<Cart> {
  return request<Cart>(`/api/users/me/cart/items/${itemId}`, {
    method: 'DELETE',
  });
}

export function clearCart(): Promise<boolean> {
  return request<boolean>('/api/users/me/cart', {
    method: 'DELETE',
  });
}
