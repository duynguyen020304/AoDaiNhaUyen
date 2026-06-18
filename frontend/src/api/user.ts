import { request, requestPaginated } from './client';
import type { UserProfile, UpdateProfilePayload } from '../types/user';
import type { UserAddress, CreateAddressPayload, UpdateAddressPayload } from '../types/address';
import type { UserOrder } from '../types/order';

export function getUserProfile(): Promise<UserProfile> {
  return request<UserProfile>('/api/users/me/profile');
}

export function updateProfile(payload: UpdateProfilePayload): Promise<UserProfile> {
  return request<UserProfile>('/api/users/me/profile', {
    method: 'PUT',
    body: JSON.stringify(payload),
  });
}

export function getAddresses(): Promise<UserAddress[]> {
  return request<UserAddress[]>('/api/users/me/addresses');
}

export function createAddress(payload: CreateAddressPayload): Promise<UserAddress> {
  return request<UserAddress>('/api/users/me/addresses', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}

export function updateAddress(id: string, payload: UpdateAddressPayload): Promise<UserAddress> {
  return request<UserAddress>(`/api/users/me/addresses/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  });
}

export function deleteAddress(id: string): Promise<void> {
  return request<void>(`/api/users/me/addresses/${id}`, { method: 'DELETE' });
}

export async function getOrders(): Promise<UserOrder[]> {
  const response = await requestPaginated<UserOrder[]>('/api/users/me/orders');
  return response.data;
}

export async function cancelOrder(orderId: string): Promise<void> {
  await request<void>(`/api/users/me/orders/${orderId}/cancel`, { method: 'PATCH' });
}
