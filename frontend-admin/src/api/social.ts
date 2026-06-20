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
  platformPostId: string | null
  publishedAt: string | null
  platformPostUrl: string | null
  errorMessage: string | null
}

export interface SocialPostMedia {
  type: string | null
  url: string
}

export interface SocialPost {
  id: string
  content: string | null
  status: string | null
  scheduledFor: string | null
  publishedAt: string | null
  platformPostUrl: string | null
  platforms: SocialPostPlatform[]
  mediaItems: SocialPostMedia[]
}

export interface UpdateSocialPostRequest {
  content?: string | null
  publishNow?: boolean | null
  scheduledFor?: string | null
  accountIds?: string[] | null
  mediaUrls?: string[] | null
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

export const getSocialPost = (id: string) =>
  request<SocialPost>(`/api/admin/social/posts/${encodeURIComponent(id)}`)

export const updateSocialPost = (id: string, data: UpdateSocialPostRequest) =>
  request<SocialPost>(`/api/admin/social/posts/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })

export interface SocialPostActionResult {
  success: boolean
  message: string | null
}

export const deleteSocialPost = (id: string) =>
  request<void>(`/api/admin/social/posts/${encodeURIComponent(id)}`, { method: 'DELETE' })

export const unpublishSocialPost = (id: string, platform = 'facebook') =>
  request<SocialPostActionResult>(`/api/admin/social/posts/${encodeURIComponent(id)}/unpublish`, {
    method: 'POST',
    body: JSON.stringify({ platform }),
  })

export const getSocialAnalytics = (platform = 'facebook', fromDate?: string, toDate?: string) =>
  request<SocialAnalytics>(`/api/admin/social/analytics?${qs({ platform, fromDate, toDate })}`)

export interface SocialCommentedPost {
  id: string
  platform: string
  accountId: string
  accountUsername: string | null
  content: string | null
  picture: string | null
  permalink: string | null
  createdTime: string | null
  commentCount: number
  likeCount: number
}

export interface SocialCommentedPostList {
  items: SocialCommentedPost[]
  nextCursor: string | null
  hasMore: boolean
}

export interface SocialCommentAuthor {
  id: string | null
  name: string | null
  username: string | null
  picture: string | null
  isOwner: boolean
}

export interface SocialComment {
  id: string
  parentId: string | null
  author: SocialCommentAuthor | null
  message: string | null
  createdTime: string | null
  likeCount: number
  replyCount: number
  platform: string | null
  url: string | null
  canReply: boolean
  canDelete: boolean
  canHide: boolean
  isHidden: boolean
  replies: SocialComment[]
}

export interface SocialCommentList {
  items: SocialComment[]
  nextCursor: string | null
  hasMore: boolean
}

export interface SocialConversation {
  id: string
  platform: string
  accountId: string
  accountUsername: string | null
  participantId: string | null
  participantName: string | null
  participantPicture: string | null
  lastMessage: string | null
  updatedTime: string | null
  status: string | null
  unreadCount: number | null
  url: string | null
}

export interface SocialConversationList {
  items: SocialConversation[]
  nextCursor: string | null
  hasMore: boolean
}

export interface SocialMessageAttachment {
  id: string | null
  type: string | null
  url: string | null
  fileName: string | null
  previewUrl: string | null
}

export interface SocialMessage {
  id: string
  conversationId: string
  accountId: string
  platform: string | null
  text: string | null
  senderId: string | null
  senderName: string | null
  direction: string
  createdAt: string | null
  attachments: SocialMessageAttachment[]
}

export interface SocialMessageList {
  items: SocialMessage[]
  nextCursor: string | null
  hasMore: boolean
}

export const getSocialCommentedPosts = (platform = 'facebook', accountId?: string, profileId?: string, cursor?: string | null, limit = 25) =>
  request<SocialCommentedPostList>(`/api/admin/social/comments?${qs({ platform, accountId, profileId, cursor, limit })}`)

export const getSocialComments = (postId: string, accountId: string, cursor?: string | null, limit = 50) =>
  request<SocialCommentList>(`/api/admin/social/comments/${encodeURIComponent(postId)}?${qs({ accountId, cursor, limit })}`)

export const replySocialComment = (postId: string, accountId: string, message: string, commentId?: string | null) =>
  request<SocialPostActionResult>(`/api/admin/social/comments/${encodeURIComponent(postId)}`, {
    method: 'POST',
    body: JSON.stringify({ accountId, message, commentId }),
  })

export const deleteSocialComment = (postId: string, accountId: string, commentId: string) =>
  request<SocialPostActionResult>(`/api/admin/social/comments/${encodeURIComponent(postId)}/${encodeURIComponent(commentId)}?${qs({ accountId })}`, { method: 'DELETE' })

export const toggleSocialCommentHidden = (postId: string, accountId: string, commentId: string, isHidden: boolean) =>
  request<SocialPostActionResult>(`/api/admin/social/comments/${encodeURIComponent(postId)}/${encodeURIComponent(commentId)}/visibility?${qs({ accountId })}`, {
    method: 'PATCH',
    body: JSON.stringify({ isHidden }),
  })

export const getSocialConversations = (platform = 'facebook', accountId?: string, profileId?: string, cursor?: string | null, limit = 25) =>
  request<SocialConversationList>(`/api/admin/social/conversations?${qs({ platform, accountId, profileId, cursor, limit })}`)

export const getSocialConversationMessages = (conversationId: string, accountId: string, cursor?: string | null, limit = 50) =>
  request<SocialMessageList>(`/api/admin/social/conversations/${encodeURIComponent(conversationId)}/messages?${qs({ accountId, cursor, limit })}`)

export const sendSocialConversationMessage = (conversationId: string, accountId: string, data: { message?: string | null; attachmentUrl?: string | null; attachmentType?: string | null }) =>
  request<SocialPostActionResult>(`/api/admin/social/conversations/${encodeURIComponent(conversationId)}/messages`, {
    method: 'POST',
    body: JSON.stringify({ accountId, ...data }),
  })

export const markSocialConversationRead = (conversationId: string, accountId: string) =>
  request<SocialPostActionResult>(`/api/admin/social/conversations/${encodeURIComponent(conversationId)}/read`, {
    method: 'POST',
    body: JSON.stringify({ accountId }),
  })

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
