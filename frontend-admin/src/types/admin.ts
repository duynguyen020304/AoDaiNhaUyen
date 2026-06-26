export interface AdminUserListItem {
  id: string
  fullName: string
  email: string | null
  phone: string | null
  status: string
  roles: string[]
  createdAt: string
  lastLoginAt: string | null
  isDeleted: boolean
}

export interface RoleDto {
  id: string
  name: string
  description: string | null
}

export interface CreateUserRequest {
  fullName: string
  email?: string
  phone?: string
  password?: string
  roleId?: string
}

export interface UpdateUserRequest {
  fullName: string
  email?: string
  phone?: string
}

export interface UpdateUserRoleRequest {
  roleId: string
}

export interface UpdateUserStatusRequest {
  status: string
}

export interface AdminOrderListItem {
  id: string
  orderCode: string
  customerName: string
  totalAmount: number
  status: string
  createdAt: string
  completedAt: string | null
}

export interface AdminOrderItemDetail {
  id: string
  productId: string | null
  variantId: string | null
  productName: string
  sku: string | null
  size: string | null
  color: string | null
  unitPrice: number
  quantity: number
  lineTotal: number
}

export interface AdminOrderDetail {
  id: string
  orderCode: string
  customerName: string | null
  customerEmail: string | null
  province: string | null
  district: string | null
  ward: string | null
  addressLine: string | null
  subtotal: number
  discountAmount: number
  shippingFee: number
  totalAmount: number
  orderStatus: string
  note: string | null
  createdAt: string
  items: AdminOrderItemDetail[]
}

// ── Products ──

export interface AdminProductListItem {
  id: string
  name: string
  slug: string
  productType: string
  categoryName: string
  status: string
  isFeatured: boolean
  variantCount: number
  totalStock: number
  isDeleted: boolean
  createdAt: string
}

export interface AdminVariantResponse {
  id: string
  sku: string
  variantName: string | null
  size: string | null
  color: string | null
  price: number
  salePrice: number | null
  stockQty: number
  isDefault: boolean
  status: string
}

export interface AdminImageResponse {
  id: string
  imageUrl: string
  altText: string | null
  sortOrder: number
  isPrimary: boolean
  isPublic: boolean
}

export interface AdminProductDetail {
  id: string
  name: string
  slug: string
  productType: string
  categoryId: string
  categoryName: string
  shortDescription: string | null
  description: string | null
  material: string | null
  brand: string | null
  origin: string | null
  careInstruction: string | null
  status: string
  isFeatured: boolean
  createdAt: string
  updatedAt: string
  variants: AdminVariantResponse[]
  images: AdminImageResponse[]
}

export interface CreateProductRequest {
  name: string
  slug: string
  productType: string
  categoryId: string
  shortDescription?: string
  description?: string
  material?: string
  brand?: string
  origin?: string
  careInstruction?: string
  status: string
  isFeatured: boolean
}

export interface UpdateProductRequest {
  name: string
  slug: string
  productType: string
  categoryId: string
  shortDescription?: string
  description?: string
  material?: string
  brand?: string
  origin?: string
  careInstruction?: string
  status: string
  isFeatured: boolean
}

export interface CreateVariantRequest {
  sku: string
  variantName?: string | null
  size?: string | null
  color?: string | null
  price: number
  salePrice?: number | null
  stockQty: number
  isDefault: boolean
  status: string
}

export interface UpdateVariantRequest {
  sku: string
  variantName?: string | null
  size?: string | null
  color?: string | null
  price: number
  salePrice?: number | null
  stockQty: number
  isDefault: boolean
  status: string
}

export interface UpdateVariantStockRequest {
  stockQty: number
}

// ── Categories ──

export interface CategoryListItem {
  id: string
  parent: string | null
  name: string
  slug: string
  description: string | null
  imageUrl: string | null
  sortOrder: number
  productCount: number
  isDeleted: boolean
  createdAt: string
}

export interface CategoryDetail {
  id: string
  parent: string | null
  name: string
  slug: string
  description: string | null
  imageUrl: string | null
  sortOrder: number
  createdAt: string
  updatedAt: string
}

export interface CreateCategoryRequest {
  name: string
  slug: string
  parent?: string | null
  description?: string
  imageUrl?: string
  sortOrder?: number
}

export interface UpdateCategoryRequest {
  name: string
  slug: string
  parent?: string | null
  description?: string
  imageUrl?: string
  sortOrder?: number
}

export interface CreateRoleRequest {
  name: string
  description?: string
}

export interface UpdateRoleRequest {
  name: string
  description?: string
}

// ── Promos ──

export interface AdminPromoItem {
  id: string
  code: string
  discountType: 'percentage' | 'fixed'
  discountValue: number
  minOrderAmount: number
  maxUses: number
  currentUses: number
  isActive: boolean
  isDeleted: boolean
  freeShipping: boolean
  startDate: string
  endDate: string
  createdAt: string
  updatedAt: string
}

export interface CreatePromoRequest {
  code: string
  discountType: 'percentage' | 'fixed'
  discountValue: number
  minOrderAmount?: number
  maxUses?: number
  startDate?: string
  endDate?: string
  freeShipping?: boolean
  isActive?: boolean
}

export interface UpdatePromoRequest {
  code: string
  discountType: 'percentage' | 'fixed'
  discountValue: number
  minOrderAmount: number
  maxUses: number
  startDate?: string
  endDate?: string
  freeShipping: boolean
  isActive: boolean
}

export interface TogglePromoStatusRequest {
  isActive: boolean
}

// ── Reviews ──

export interface AdminReviewItem {
  id: string
  userId: string
  userName: string | null
  userEmail: string | null
  productId: string
  productName: string | null
  rating: number
  content: string
  isVisible: boolean
  replyCount: number
  createdAt: string
}

export interface SetReviewVisibilityRequest {
  isVisible: boolean
}

export interface AdminAuditLogItem {
  id: string
  actorUserId: string | null
  actorName: string | null
  actorEmail: string | null
  actorRoles: string | null
  httpMethod: string
  path: string
  actionType: string
  entityType: string
  entityId: string | null
  statusCode: number
  success: boolean
  createdAt: string
  requestPreview: string | null
  responsePreview: string | null
  error: string | null
}

export interface AdminAuditLogDetail {
  id: string
  actorUserId: string | null
  actorName: string | null
  actorEmail: string | null
  actorRoles: string | null
  httpMethod: string
  path: string
  queryString: string | null
  controllerName: string | null
  actionName: string | null
  actionType: string
  entityType: string
  entityId: string | null
  statusCode: number
  success: boolean
  requestPreview: string | null
  responsePreview: string | null
  error: string | null
  ipAddressHash: string | null
  userAgentHash: string | null
  createdAt: string
}

export interface AdminAuditLogStats {
  total: number
  success: number
  failed: number
  distinctActors: number
  distinctEntities: number
}

export interface ReplyToReviewRequest {
  productId: string
  content: string
}

// ── AI Try-on Feedback ──

export interface AdminAiTryOnFeedbackItem {
  id: string
  generatedImageId: string
  imageUrl: string
  userId: string | null
  userName: string | null
  userEmail: string | null
  rating: number
  comment: string | null
  adminNote: string | null
  isResolved: boolean
  createdAt: string
}

export interface UpdateAiTryOnFeedbackStatusRequest {
  isResolved: boolean
  adminNote?: string | null
}

// ── Orders ──

export interface AdminOrderListItem {
  orderCode: string
  customerName: string
  totalAmount: number
  status: string
  createdAt: string
}

// ── Email Marketing ──

export type EmailTemplateType = 'marketing.promo' | 'marketing.newsletter' | 'subscriber.welcome' | 'order.confirmation' | 'legacy.html'
export interface EmailTemplateListItem { id: string; key: string; name: string; subject: string; templateType: EmailTemplateType | string; locale: string; version: number; isSystem: boolean; isActive: boolean; isDeleted: boolean; createdAt: string; updatedAt: string }
export interface EmailTemplateDetail extends EmailTemplateListItem { preheader: string | null; configJson: string }
export interface CreateEmailTemplateRequest { key: string; name: string; subject: string; preheader?: string; templateType: EmailTemplateType; configJson: string; locale: string; isSystem: boolean }
export interface UpdateEmailTemplateRequest { name: string; subject: string; preheader?: string; templateType: EmailTemplateType; configJson: string; locale: string; isSystem: boolean; isActive: boolean }
export interface SubscriberListItem { id: string; email: string; status: string; subscribedAt: string | null; unsubscribedAt: string | null; lastSentAt: string | null; userId: string | null; isDeleted: boolean }
export interface ConsentRecord { channel: string; isOptIn: boolean; source: string; consentedAt: string | null; revokedAt: string | null }
export interface SubscriberDetail extends SubscriberListItem { lastOpenAt: string | null; lastClickAt: string | null; consents: ConsentRecord[] }
export interface ImportSubscribersRequest { emails: string[]; source: string }
export interface ImportSubscribersResult { imported: number; skipped: number }
export interface EmailJobListItem { id: string; toEmail: string; templateKey: string; status: string; retryCount: number; scheduledAt: string; sentAt: string | null; errorMessage: string | null }
export interface SendLogRecord { status: string; sentAt: string | null; failedAt: string | null; errorMessage: string | null }
export interface EmailJobDetail extends EmailJobListItem { payloadJson: string; logs: SendLogRecord[] }
export interface MarketingStats { totalSubscribers: number; activeSubscribers: number; pendingSubscribers: number; unsubscribedSubscribers: number; queuedJobs: number; sentJobsToday: number; failedJobs: number; templateCount: number }
export interface MarketingContentOption { id: string; type: 'promo' | 'blog' | 'product' | string; title: string; subtitle: string | null; url: string | null; badge: string | null; htmlSnippet: string }
export interface MarketingCampaignAttachmentRequest { type: string; id?: string | null; title: string; url?: string | null; description?: string | null; code?: string | null }
export interface SendMarketingCampaignRequest { recipientMode: 'all_active' | 'selected' | 'manual'; subscriberIds?: string[]; manualEmails?: string[]; templateKey: string; subject: string; preheader?: string | null; intro?: string | null; bodyHtml?: string | null; ctaLabel?: string | null; ctaUrl?: string | null; attachments?: MarketingCampaignAttachmentRequest[]; scheduledAt?: string | null }
export interface MarketingCampaignSendResult { queued: number; skipped: number; skippedEmails: string[] }
