import { request, requestPaginated } from './client';
import type { Review, PaginatedReviews } from '../types/catalog';

export function getProductReviews(
  productId: string,
  page = 1,
  pageSize = 10,
): Promise<PaginatedReviews> {
  return requestPaginated<Review[]>(
    `/api/v1/products/${productId}/reviews?page=${page}&pageSize=${pageSize}`,
  );
}

export function createReview(
  productId: string,
  rating: number,
  comment?: string,
): Promise<Review> {
  return request<Review>(`/api/v1/products/${productId}/reviews`, {
    method: 'POST',
    body: JSON.stringify({ rating, comment: comment || null }),
  });
}
