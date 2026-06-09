import { z } from 'zod'

export const blogTemplates = ['StandardArticle', 'PhotoGallery', 'VideoFeature', 'ProductSpotlight', 'HowTo'] as const
export const blogStatuses = ['Draft', 'Published', 'Archived'] as const

export type BlogTemplate = typeof blogTemplates[number]
export type BlogStatus = typeof blogStatuses[number]

export type BlogBlock =
  | { type: 'heading'; level: 1 | 2 | 3; content: string }
  | { type: 'paragraph'; content: string }
  | { type: 'image'; src: string; alt: string; caption?: string; width?: 'full' | 'contained'; widthPx?: number; heightPx?: number }
  | { type: 'gallery'; images: { src: string; alt: string; caption?: string; widthPx?: number; heightPx?: number }[] }
  | { type: 'video'; src: string; poster?: string; caption?: string }
  | { type: 'product_spotlight'; productSlugs: string[] }
  | { type: 'step'; stepNumber: number; title: string; content: string; tip?: string }
  | { type: 'quote'; content: string; attribution?: string }
  | { type: 'divider' }
  | { type: 'callout'; variant: 'info' | 'warning' | 'tip'; content: string }
  | { type: 'code'; language: string; content: string }
  | { type: 'embed'; url: string; caption?: string }

export interface BlogPostListItem {
  id: string
  title: string
  slug: string
  excerpt: string
  featuredImage: string | null
  featuredImageWidth: number | null
  featuredImageHeight: number | null
  template: BlogTemplate
  tags: string[]
  authorName: string | null
  status: BlogStatus
  publishedAt: string | null
  updatedAt: string
}

export interface BlogPost extends BlogPostListItem {
  content: BlogBlock[]
  authorId: string | null
  authorAvatarUrl: string | null
  authorBio: string | null
  reviewedBy: string | null
  informationGain: string | null
  metaTitle: string | null
  metaDescription: string | null
  canonicalUrl: string | null
  createdAt: string
}

export interface BlogPostPayload {
  title: string
  slug?: string | null
  excerpt: string
  featuredImage?: string | null
  featuredImageWidth?: number | null
  featuredImageHeight?: number | null
  template: BlogTemplate
  content: BlogBlock[]
  tags: string[]
  authorId?: string | null
  authorNameOverride?: string | null
  authorBio?: string | null
  reviewedBy?: string | null
  informationGain?: string | null
  status: BlogStatus
  publishedAt?: string | null
  metaTitle?: string | null
  metaDescription?: string | null
  canonicalUrl?: string | null
}

export const BlogPayloadSchema = z.object({
  title: z.string().trim().min(1, 'Tiêu đề không được để trống').max(500),
  slug: z.string().trim().max(500).nullable().optional(),
  excerpt: z.string().trim().min(1, 'Tóm tắt không được để trống'),
  featuredImage: z.string().trim().max(1000).nullable().optional(),
  featuredImageWidth: z.number().int().positive().nullable().optional(),
  featuredImageHeight: z.number().int().positive().nullable().optional(),
  template: z.enum(blogTemplates),
  content: z.array(z.looseObject({ type: z.string() })).min(1, 'Cần ít nhất 1 block nội dung'),
  tags: z.array(z.string().trim()).default([]),
  authorId: z.string().uuid().nullable().optional(),
  authorNameOverride: z.string().trim().max(200).nullable().optional(),
  authorBio: z.string().trim().nullable().optional(),
  reviewedBy: z.string().trim().max(200).nullable().optional(),
  informationGain: z.string().trim().nullable().optional(),
  status: z.enum(blogStatuses),
  publishedAt: z.string().nullable().optional(),
  metaTitle: z.string().trim().max(200).nullable().optional(),
  metaDescription: z.string().trim().max(500).nullable().optional(),
  canonicalUrl: z.string().trim().nullable().optional(),
})
