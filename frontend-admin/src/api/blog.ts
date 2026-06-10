import { request, requestPaginated } from '@/api/client'
import type { BlogBlock, BlogCategory, BlogPost, BlogPostListItem, BlogPostPayload, BlogStatus } from '@/types/blog'

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

interface RawBlogBlock {
  type: string
  data?: Record<string, unknown>
}

function normalizeBlogBlocks(blocks: RawBlogBlock[]): BlogBlock[] {
  if (!Array.isArray(blocks)) return []
  return blocks.map((block) => {
    if (block && block.data && typeof block.data === 'object') {
      return {
        ...block,
        ...block.data,
      } as unknown as BlogBlock
    }
    return block as unknown as BlogBlock
  })
}

export async function getBlogPosts(params: BlogListParams = {}) {
  return requestPaginated<BlogPostListItem[]>(`/api/v1/admin/blog${qs(params)}`)
}

export async function getBlogPost(id: string) {
  const post = await request<BlogPost>(`/api/v1/admin/blog/${id}`)
  if (post && post.content) {
    post.content = normalizeBlogBlocks(post.content)
  }
  return post
}

export async function createBlogPost(data: BlogPostPayload) {
  const post = await request<BlogPost>('/api/v1/admin/blog', { method: 'POST', body: JSON.stringify(data) })
  if (post && post.content) {
    post.content = normalizeBlogBlocks(post.content)
  }
  return post
}

export async function updateBlogPost(id: string, data: BlogPostPayload) {
  const post = await request<BlogPost>(`/api/v1/admin/blog/${id}`, { method: 'PUT', body: JSON.stringify(data) })
  if (post && post.content) {
    post.content = normalizeBlogBlocks(post.content)
  }
  return post
}

export function deleteBlogPost(id: string) {
  return request<void>(`/api/v1/admin/blog/${id}`, { method: 'DELETE' })
}

export async function getBlogCategories() {
  return request<BlogCategory[]>('/api/v1/blog/categories')
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
