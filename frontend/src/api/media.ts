import { request } from './client';

export interface UserImage {
  id: string;
  objectKey: string;
  url: string;
  kind: string;
  mimeType: string;
  originalFileName: string | null;
  fileSizeBytes: number;
  sourceType: string;
  createdAt: string;
}

export interface UserImageListData {
  items: UserImage[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export async function getMyImages(
  page = 1,
  pageSize = 12,
  sourceType?: string,
): Promise<UserImageListData> {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });
  if (sourceType) {
    params.set('sourceType', sourceType);
  }
  return request<UserImageListData>(`/api/v1/media/my-images?${params}`);
}

export async function getImagePresignedUrl(
  id: string,
): Promise<{ url: string; mimeType: string; originalFileName: string | null }> {
  return request<{ url: string; mimeType: string; originalFileName: string | null }>(
    `/api/v1/media/${id}/url`,
  );
}
