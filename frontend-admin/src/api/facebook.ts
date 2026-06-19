import { request } from './client'

function qs(params: Record<string, string | number | boolean | undefined | null>) {
  const search = new URLSearchParams()
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  })
  return search.toString()
}

export interface ConnectFacebookPageRequest {
  pageId: string
  pageAccessToken: string
  pageName?: string
}

export interface FacebookConnection {
  pageId: string
  pageName: string | null
  tokenLast4: string
  expiresAt: string | null
  lastValidatedAt: string | null
  isActive: boolean
}

export interface FacebookPageInfo {
  pageId: string
  name: string
  category: string | null
  link: string | null
}

export interface CreateFacebookPostRequest {
  message: string
  link?: string
  scheduledPublishTime?: string
  published: boolean
}

export interface UpdateFacebookPostRequest {
  message: string
}

export interface FacebookPost {
  id: string
  message: string | null
  createdTime: string | null
  updatedTime: string | null
  permalinkUrl: string | null
  fullPicture: string | null
  isPublished: boolean | null
  scheduledPublishTime: string | null
  statusType: string | null
  type: string | null
}

export interface FacebookPostList {
  items: FacebookPost[]
  beforeCursor: string | null
  afterCursor: string | null
  nextUrl: string | null
}

export interface FacebookPublishResult {
  id: string
  postId: string | null
  permalinkUrl: string | null
}

export const getFacebookConnections = () =>
  request<FacebookConnection[]>('/api/admin/facebook/connections')

export const connectFacebookPage = (data: ConnectFacebookPageRequest) =>
  request<FacebookConnection>('/api/admin/facebook/connections', {
    method: 'POST',
    body: JSON.stringify(data),
  })

export const disconnectFacebookPage = (pageId: string) =>
  request<void>(`/api/admin/facebook/connections/${encodeURIComponent(pageId)}`, {
    method: 'DELETE',
  })

export const getFacebookPageInfo = (pageId: string) =>
  request<FacebookPageInfo>(`/api/admin/facebook/${encodeURIComponent(pageId)}/info`)

export const getFacebookPosts = (pageId: string, cursor?: string | null, limit = 25) =>
  request<FacebookPostList>(
    `/api/admin/facebook/${encodeURIComponent(pageId)}/posts?${qs({ cursor, limit })}`,
  )

export const publishFacebookPost = (pageId: string, data: CreateFacebookPostRequest) =>
  request<FacebookPublishResult>(`/api/admin/facebook/${encodeURIComponent(pageId)}/posts`, {
    method: 'POST',
    body: JSON.stringify(data),
  })

export const publishFacebookPhoto = (
  pageId: string,
  file: File,
  caption: string,
  scheduledPublishTime?: string,
  published = true,
) => {
  const form = new FormData()
  form.set('file', file)
  if (caption.trim()) form.set('caption', caption.trim())
  if (scheduledPublishTime) form.set('scheduledPublishTime', scheduledPublishTime)
  form.set('published', String(published))
  return request<FacebookPublishResult>(`/api/admin/facebook/${encodeURIComponent(pageId)}/photos`, {
    method: 'POST',
    body: form,
  })
}

export const publishFacebookVideo = (
  pageId: string,
  file: File,
  description: string,
  scheduledPublishTime?: string,
  published = true,
) => {
  const form = new FormData()
  form.set('file', file)
  if (description.trim()) form.set('description', description.trim())
  if (scheduledPublishTime) form.set('scheduledPublishTime', scheduledPublishTime)
  form.set('published', String(published))
  return request<FacebookPublishResult>(`/api/admin/facebook/${encodeURIComponent(pageId)}/videos`, {
    method: 'POST',
    body: form,
  })
}

export const updateFacebookPost = (postId: string, data: UpdateFacebookPostRequest) =>
  request<FacebookPost>(`/api/admin/facebook/posts/${encodeURIComponent(postId)}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })

export const deleteFacebookPost = (postId: string) =>
  request<void>(`/api/admin/facebook/posts/${encodeURIComponent(postId)}`, {
    method: 'DELETE',
  })
