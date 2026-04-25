import { request } from './client';

export interface AiTryOnCatalogItem {
  productId: number;
  defaultVariantId: number | null;
  name: string;
  productType: string;
  categorySlug: string;
  thumbnailUrl: string;
  aiAssetUrl: string;
  isFeatured: boolean;
}

export interface AiTryOnCatalogResponse {
  garments: AiTryOnCatalogPage;
  accessories: AiTryOnCatalogPage;
  garmentCategories: AiTryOnCatalogCategory[];
  accessoryCategories: AiTryOnCatalogCategory[];
}

export interface AiTryOnCatalogPage {
  items: AiTryOnCatalogItem[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface AiTryOnCatalogCategory {
  key: string;
  label: string;
}

export interface AiTryOnResponse {
  resultImageUrl: string;
  mimeType: string;
}

interface SubmitAiTryOnParams {
  personImage: File;
  garmentProductId: number;
  garmentVariantId?: number | null;
  accessoryProductIds?: number[];
}

interface GetAiTryOnCatalogParams {
  garmentPage?: number;
  accessoryPage?: number;
  pageSize?: number;
  garmentCategory?: string;
  accessoryCategory?: string;
}

export function getAiTryOnCatalog({
  garmentPage = 1,
  accessoryPage = 1,
  pageSize = 6,
  garmentCategory = 'all',
  accessoryCategory = 'all',
}: GetAiTryOnCatalogParams = {}): Promise<AiTryOnCatalogResponse> {
  const params = new URLSearchParams({
    garmentPage: String(garmentPage),
    accessoryPage: String(accessoryPage),
    pageSize: String(pageSize),
    garmentCategory,
    accessoryCategory,
  });

  return request<AiTryOnCatalogResponse>(`/api/v1/ai-tryon/catalog?${params}`);
}

export function submitAiTryOn({
  personImage,
  garmentProductId,
  garmentVariantId,
  accessoryProductIds = [],
}: SubmitAiTryOnParams): Promise<AiTryOnResponse> {
  const formData = new FormData();
  formData.append('personImage', personImage);
  formData.append('garmentProductId', String(garmentProductId));

  if (typeof garmentVariantId === 'number') {
    formData.append('garmentVariantId', String(garmentVariantId));
  }

  accessoryProductIds.forEach((productId) => {
    formData.append('accessoryProductIds', String(productId));
  });

  return request<AiTryOnResponse>('/api/v1/ai-tryon', {
    method: 'POST',
    body: formData,
  });
}
