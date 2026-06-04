import { request } from './client'

export interface UserImage {
  id: string
  objectKey: string
  url: string
  kind: string
  mimeType: string
  originalFileName: string | null
  fileSizeBytes: number
  sourceType: string
  createdAt: string
}

export interface UserImageListData {
  items: UserImage[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export interface MediaStats {
  totalImages: number
  totalSizeBytes: number
  chatImages: number
  aiTryOnImages: number
}

export async function getAllImages(
  page = 1,
  pageSize = 20,
  sourceType?: string,
  search?: string,
): Promise<UserImageListData> {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  })
  if (sourceType) params.set('sourceType', sourceType)
  if (search) params.set('search', search)
  return request<UserImageListData>(`/api/admin/media?${params}`)
}

export async function getImageDetail(id: string): Promise<UserImage> {
  return request<UserImage>(`/api/admin/media/${id}`)
}

export async function deleteImage(id: string): Promise<boolean> {
  return request<boolean>(`/api/admin/media/${id}`, { method: 'DELETE' })
}

export async function getMediaStats(): Promise<MediaStats> {
  return request<MediaStats>('/api/admin/media/stats')
}
