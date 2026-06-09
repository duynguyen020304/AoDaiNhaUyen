import { request, requestPaginated } from '@/api/client'
import type { BlogPost, BlogPostListItem, BlogPostPayload, BlogStatus } from '@/types/blog'

export interface BlogListParams {
  status?: BlogStatus | ''
  tag?: string
  search?: string
  page?: number
  pageSize?: number
}

function qs(params: BlogListParams): string {
  const sp = new URLSearchParams()
  if (params.status) sp.set('status', params.status)
  if (params.tag) sp.set('tag', params.tag)
  if (params.search) sp.set('search', params.search)
  if (params.page) sp.set('page', String(params.page))
  if (params.pageSize) sp.set('pageSize', String(params.pageSize))
  const value = sp.toString()
  return value ? `?${value}` : ''
}

export async function getBlogPosts(params: BlogListParams = {}) {
  return requestPaginated<BlogPostListItem[]>(`/api/v1/admin/blog${qs(params)}`)
}

export function getBlogPost(id: string) {
  return request<BlogPost>(`/api/v1/admin/blog/${id}`)
}

export function createBlogPost(data: BlogPostPayload) {
  return request<BlogPost>('/api/v1/admin/blog', { method: 'POST', body: JSON.stringify(data) })
}

export function updateBlogPost(id: string, data: BlogPostPayload) {
  return request<BlogPost>(`/api/v1/admin/blog/${id}`, { method: 'PUT', body: JSON.stringify(data) })
}

export function deleteBlogPost(id: string) {
  return request<void>(`/api/v1/admin/blog/${id}`, { method: 'DELETE' })
}

export interface BlogImageUploadResponse {
  imageId: string
  url: string
  objectKey: string
  width: number | null
  height: number | null
}

export function uploadBlogImage(file: File) {
  const formData = new FormData()
  formData.append('file', file)
  return request<BlogImageUploadResponse>('/api/v1/admin/blog/upload', { method: 'POST', body: formData })
}
