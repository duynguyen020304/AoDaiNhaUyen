import { request, requestPaginated } from './client';
import type { UserProfile, UpdateProfilePayload } from '../types/user';
import type { UserAddress, CreateAddressPayload } from '../types/address';
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

export function deleteAddress(id: number): Promise<void> {
  return request<void>(`/api/users/me/addresses/${id}`, { method: 'DELETE' });
}

export async function getOrders(): Promise<UserOrder[]> {
  const response = await requestPaginated<UserOrder[]>('/api/users/me/orders');
  return response.data;
}
