export type BlogTemplate = 'StandardArticle' | 'PhotoGallery' | 'VideoFeature' | 'ProductSpotlight' | 'HowTo';
export type BlogStatus = 'Draft' | 'Published' | 'Archived';
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
  | { type: 'embed'; url: string; caption?: string };
export interface BlogPostListItem { id: string; title: string; slug: string; excerpt: string; featuredImage: string | null; featuredImageWidth: number | null; featuredImageHeight: number | null; template: BlogTemplate; tags: string[]; authorName: string | null; status: BlogStatus; publishedAt: string | null; updatedAt: string }
export interface BlogPost extends BlogPostListItem { content: BlogBlock[]; authorId: string | null; authorAvatarUrl: string | null; authorBio: string | null; reviewedBy: string | null; informationGain: string | null; metaTitle: string | null; metaDescription: string | null; canonicalUrl: string | null; createdAt: string }
export interface BlogListParams { tag?: string; search?: string; page?: number; pageSize?: number }
