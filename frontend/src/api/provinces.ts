import { request } from './client';

export interface ProvinceOption {
  code: number;
  name: string;
}

export type WardOption = ProvinceOption;

export async function getProvinces(): Promise<ProvinceOption[]> {
  return request<ProvinceOption[]>('/api/locations/provinces');
}

export async function getWardsByProvince(provinceCode: number): Promise<WardOption[]> {
  return request<WardOption[]>(`/api/locations/provinces/${provinceCode}/wards`);
}
