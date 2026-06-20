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

export interface ConnectFacebookOAuthPageRequest {
  pageId: string
  connectToken: string
}

export interface FacebookOAuthPagesRequest {
  code: string
  redirectUri: string
}

export interface FacebookOAuthUrl {
  url: string
}

export interface FacebookOAuthPage {
  pageId: string
  name: string
  category: string | null
  tasks: string[]
  connectToken: string
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

export const getFacebookOAuthUrl = (redirectUri: string, state: string) =>
  request<FacebookOAuthUrl>(`/api/admin/facebook/oauth-url?${qs({ redirectUri, state })}`)

export const getFacebookOAuthPages = (data: FacebookOAuthPagesRequest) =>
  request<FacebookOAuthPage[]>('/api/admin/facebook/oauth/pages', {
    method: 'POST',
    body: JSON.stringify(data),
  })

export const connectFacebookOAuthPage = (data: ConnectFacebookOAuthPageRequest) =>
  request<FacebookConnection>('/api/admin/facebook/connections/oauth', {
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

export interface FacebookCommentAuthor {
  id: string | null
  name: string | null
  avatarUrl: string | null
}

export interface FacebookComment {
  id: string
  postId: string | null
  parentId: string | null
  author: FacebookCommentAuthor | null
  message: string | null
  createdTime: string | null
  likeCount: number | null
  commentCount: number | null
  canReply: boolean | null
  canHide: boolean | null
  canDelete: boolean | null
  isHidden: boolean | null
  replies: FacebookComment[]
}

export interface FacebookCommentList {
  items: FacebookComment[]
  beforeCursor: string | null
  afterCursor: string | null
  nextUrl: string | null
}

export interface FacebookCommentActionResult {
  success: boolean
  id: string | null
  message: string | null
}

export interface FacebookParticipant {
  id: string | null
  name: string | null
  email: string | null
  isPage: boolean
}

export interface FacebookConversation {
  id: string
  pageId: string
  customerId: string | null
  customerName: string | null
  customerAvatarUrl: string | null
  snippet: string | null
  updatedTime: string | null
  unreadCount: number | null
  messageCount: number | null
  link: string | null
  participants: FacebookParticipant[]
}

export interface FacebookConversationList {
  items: FacebookConversation[]
  beforeCursor: string | null
  afterCursor: string | null
  nextUrl: string | null
}

export interface FacebookMessageAttachment {
  type: string | null
  url: string | null
  name: string | null
  mimeType: string | null
  size: number | null
}

export interface FacebookMessage {
  id: string
  conversationId: string
  senderId: string | null
  senderName: string | null
  isFromPage: boolean
  text: string | null
  createdTime: string | null
  attachments: FacebookMessageAttachment[]
}

export interface FacebookMessageList {
  items: FacebookMessage[]
  beforeCursor: string | null
  afterCursor: string | null
  nextUrl: string | null
}

export interface SendFacebookMessageRequest {
  text?: string | null
  attachmentUrl?: string | null
  attachmentType?: 'image' | 'video' | 'audio' | 'file' | null
}

export interface FacebookMessageSendResult {
  success: boolean
  messageId: string | null
}

export const getFacebookPostComments = (pageId: string, postId: string, after?: string | null, limit = 25) =>
  request<FacebookCommentList>(`/api/admin/facebook/${encodeURIComponent(pageId)}/posts/${encodeURIComponent(postId)}/comments?${qs({ after, limit })}`)

export const commentOnFacebookPost = (pageId: string, postId: string, message: string) =>
  request<FacebookCommentActionResult>(`/api/admin/facebook/${encodeURIComponent(pageId)}/posts/${encodeURIComponent(postId)}/comments`, {
    method: 'POST',
    body: JSON.stringify({ message }),
  })

export const replyFacebookComment = (pageId: string, commentId: string, message: string) =>
  request<FacebookCommentActionResult>(`/api/admin/facebook/${encodeURIComponent(pageId)}/comments/${encodeURIComponent(commentId)}/replies`, {
    method: 'POST',
    body: JSON.stringify({ message }),
  })

export const toggleFacebookCommentHidden = (pageId: string, commentId: string, isHidden: boolean) =>
  request<FacebookCommentActionResult>(`/api/admin/facebook/${encodeURIComponent(pageId)}/comments/${encodeURIComponent(commentId)}/visibility`, {
    method: 'PATCH',
    body: JSON.stringify({ isHidden }),
  })

export const deleteFacebookComment = (pageId: string, commentId: string) =>
  request<void>(`/api/admin/facebook/${encodeURIComponent(pageId)}/comments/${encodeURIComponent(commentId)}`, { method: 'DELETE' })

export const getFacebookConversations = (pageId: string, after?: string | null, limit = 25) =>
  request<FacebookConversationList>(`/api/admin/facebook/${encodeURIComponent(pageId)}/conversations?${qs({ after, limit })}`)

export const getFacebookConversationMessages = (pageId: string, conversationId: string, before?: string | null, limit = 50) =>
  request<FacebookMessageList>(`/api/admin/facebook/${encodeURIComponent(pageId)}/conversations/${encodeURIComponent(conversationId)}/messages?${qs({ before, limit })}`)

export const sendFacebookConversationMessage = (pageId: string, conversationId: string, data: SendFacebookMessageRequest) =>
  request<FacebookMessageSendResult>(`/api/admin/facebook/${encodeURIComponent(pageId)}/conversations/${encodeURIComponent(conversationId)}/messages`, {
    method: 'POST',
    body: JSON.stringify(data),
  })

export const markFacebookConversationRead = (pageId: string, conversationId: string) =>
  request<{ success: boolean }>(`/api/admin/facebook/${encodeURIComponent(pageId)}/conversations/${encodeURIComponent(conversationId)}/read`, { method: 'POST' })
