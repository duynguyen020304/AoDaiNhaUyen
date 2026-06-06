import { request, requestPaginated } from './client';
import type { Comment, PaginatedComments } from '../types/catalog';

export function getProductComments(
  productId: string,
  page = 1,
  pageSize = 10,
): Promise<PaginatedComments> {
  return requestPaginated<Comment[]>(
    `/api/v1/products/${productId}/comments?page=${page}&pageSize=${pageSize}`,
  );
}

export function createComment(
  productId: string,
  content: string,
  options?: { rating?: number; parentCommentId?: string },
): Promise<Comment> {
  return request<Comment>(`/api/v1/products/${productId}/comments`, {
    method: 'POST',
    body: JSON.stringify({
      content,
      rating: options?.rating ?? null,
      parentCommentId: options?.parentCommentId ?? null,
    }),
  });
}
