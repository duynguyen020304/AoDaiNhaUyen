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
  garments: AiTryOnCatalogItem[];
  accessories: AiTryOnCatalogItem[];
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

export function getAiTryOnCatalog(): Promise<AiTryOnCatalogResponse> {
  return request<AiTryOnCatalogResponse>('/api/v1/ai-tryon/catalog');
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
