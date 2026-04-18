import { request } from './client';
import type { UserProfile, UpdateProfilePayload } from '../types/user';
import type { UserAddress, CreateAddressPayload } from '../types/address';
import type { UserOrder } from '../types/order';
import { mockProfile, mockAddresses, mockOrders } from '../pages/AccountPage/data';

export async function getUserProfile(): Promise<UserProfile> {
  try {
    return await request<UserProfile>('/api/users/me/profile');
  } catch {
    return mockProfile;
  }
}

export async function updateProfile(payload: UpdateProfilePayload): Promise<UserProfile> {
  try {
    return await request<UserProfile>('/api/users/me/profile', {
      method: 'PUT',
      body: JSON.stringify(payload),
    });
  } catch {
    return { ...mockProfile, ...payload };
  }
}

export async function getAddresses(): Promise<UserAddress[]> {
  try {
    return await request<UserAddress[]>('/api/users/me/addresses');
  } catch {
    return mockAddresses;
  }
}

export async function createAddress(payload: CreateAddressPayload): Promise<UserAddress> {
  try {
    return await request<UserAddress>('/api/users/me/addresses', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  } catch {
    return { id: Date.now(), isDefault: false, ward: null, ...payload };
  }
}

export async function deleteAddress(id: number): Promise<void> {
  try {
    await request<void>(`/api/users/me/addresses/${id}`, { method: 'DELETE' });
  } catch {
    // mock: no-op
  }
}

export async function getOrders(): Promise<UserOrder[]> {
  try {
    return await request<UserOrder[]>('/api/users/me/orders');
  } catch {
    return mockOrders;
  }
}
