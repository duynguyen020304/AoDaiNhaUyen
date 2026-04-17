import { request } from './client';
import type { HeaderCategory, PaginatedProducts } from '../types/catalog';

export function getHeaderCategories(): Promise<HeaderCategory[]> {
  return request<HeaderCategory[]>('/api/v1/categories/header');
}

interface GetProductsParams {
  categorySlug?: string;
  productType?: string;
  featured?: boolean;
  size?: string;
  page?: number;
  pageSize?: number;
}

export function getProducts(params: GetProductsParams = {}): Promise<PaginatedProducts> {
  const search = new URLSearchParams();

  if (params.categorySlug) {
    search.set('categorySlug', params.categorySlug);
  }

  if (params.productType) {
    search.set('productType', params.productType);
  }

  if (typeof params.featured === 'boolean') {
    search.set('featured', String(params.featured));
  }

  if (params.size) {
    search.set('size', params.size);
  }

  if (params.page) {
    search.set('page', String(params.page));
  }

  if (params.pageSize) {
    search.set('pageSize', String(params.pageSize));
  }

  const query = search.toString();
  return request<PaginatedProducts>(`/api/v1/products${query ? `?${query}` : ''}`);
}
