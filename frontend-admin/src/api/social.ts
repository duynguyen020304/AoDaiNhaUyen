import { request } from './client'

function qs(params: Record<string, string | number | boolean | undefined | null>) {
  const search = new URLSearchParams()
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  })
  return search.toString()
}

export interface SocialAccountConnection {
  id: string
  provider: string
  platform: string
  zernioProfileId: string
  zernioAccountId: string
  displayName: string | null
  username: string | null
  avatarUrl: string | null
  lastSyncedAt: string | null
  isActive: boolean
}

export interface SocialConnectUrl {
  authUrl: string
  state: string | null
}

export interface SelectFacebookPageRequest {
  profileId: string
  pageId: string
  tempToken: string
  userProfile: {
    id: string
    name: string
    profilePicture: string
  }
  redirectUrl: string
}

export interface CreateSocialPostRequest {
  content: string
  accountIds: string[]
  publishNow: boolean
  scheduledFor?: string | null
  mediaUrls?: string[]
}

export interface SocialPostPlatform {
  platform: string
  accountId: string
  status: string | null
  platformPostUrl: string | null
}

export interface SocialPost {
  id: string
  content: string | null
  status: string | null
  scheduledFor: string | null
  publishedAt: string | null
  platformPostUrl: string | null
  platforms: SocialPostPlatform[]
}

export interface SocialPostList {
  items: SocialPost[]
  page: number
  limit: number
}

export interface SocialAnalyticsMetrics {
  impressions: number
  likes: number
  comments: number
  shares: number
  clicks: number
  views: number
}

export interface SocialAnalytics {
  platform: string
  fromDate: string
  toDate: string
  posts: SocialAnalyticsMetrics
}

export const getSocialAccounts = (platform = 'facebook', sync = false, profileId?: string) =>
  request<SocialAccountConnection[]>(`/api/admin/social/accounts?${qs({ platform, sync, profileId })}`)

export const getSocialConnectUrl = (platform: string, profileId: string, redirectUrl: string, headless = false) =>
  request<SocialConnectUrl>(`/api/admin/social/connect-url?${qs({ platform, profileId, redirectUrl, headless })}`)

export const selectFacebookPage = (data: SelectFacebookPageRequest) =>
  request<SocialAccountConnection[]>('/api/admin/social/facebook/pages/select', {
    method: 'POST',
    body: JSON.stringify(data),
  })

export const disconnectSocialAccount = (id: string) =>
  request<void>(`/api/admin/social/accounts/${encodeURIComponent(id)}`, { method: 'DELETE' })

export const createSocialPost = (data: CreateSocialPostRequest) =>
  request<SocialPost>('/api/admin/social/posts', {
    method: 'POST',
    body: JSON.stringify(data),
  })

export const getSocialPosts = (platform = 'facebook', accountId?: string, profileId?: string, page = 1, limit = 25) =>
  request<SocialPostList>(`/api/admin/social/posts?${qs({ platform, accountId, profileId, page, limit })}`)

export const getSocialAnalytics = (platform = 'facebook', fromDate?: string, toDate?: string) =>
  request<SocialAnalytics>(`/api/admin/social/analytics?${qs({ platform, fromDate, toDate })}`)

export interface SocialMediaUpload {
  publicUrl: string
  objectKey: string
  fileName: string
  contentType: string
  fileSize: number
}

export const uploadSocialMedia = (file: File) => {
  const formData = new FormData()
  formData.append('file', file)
  return request<SocialMediaUpload>('/api/admin/social/media/upload', {
    method: 'POST',
    body: formData,
  })
}
