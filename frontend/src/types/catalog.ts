import type { PaginatedApiEnvelope } from './api';

export interface HeaderCategoryChild {
  id: string;
  name: string;
  slug: string;
  sortOrder: number;
}

export interface HeaderCategory {
  id: string;
  name: string;
  slug: string;
  sortOrder: number;
  children: HeaderCategoryChild[];
}

export interface ProductListItem {
  id: string;
  name: string;
  slug: string;
  productType: string;
  status: string;
  shortDescription: string | null;
  price: number;
  salePrice: number | null;
  categorySlug: string;
  isFeatured: boolean;
  stockQty: number;
  primaryImageUrl: string | null;
  primaryVariantId: string | null;
  primaryVariantSku: string | null;
}

export type PaginatedProducts = PaginatedApiEnvelope<ProductListItem[]>;
