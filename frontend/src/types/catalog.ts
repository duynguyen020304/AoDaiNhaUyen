import type { PaginatedApiEnvelope } from './api';

export interface HeaderCategoryChild {
  id: string;
  name: string;
  slug: string;
  sortOrder: number;
}

export interface HeaderCategory {
  id: string;
  name: string;
  slug: string;
  sortOrder: number;
  children: HeaderCategoryChild[];
}

export interface ProductListItem {
  id: string;
  name: string;
  slug: string;
  productType: string;
  status: string;
  shortDescription: string | null;
  price: number;
  salePrice: number | null;
  categorySlug: string;
  isFeatured: boolean;
  stockQty: number;
  primaryImageUrl: string | null;
  primaryVariantId: string | null;
  primaryVariantSku: string | null;
}

export type PaginatedProducts = PaginatedApiEnvelope<ProductListItem[]>;

export interface ProductVariant {
  id: string;
  sku: string;
  variantName: string | null;
  size: string | null;
  color: string | null;
  price: number;
  salePrice: number | null;
  stockQty: number;
  isDefault: boolean;
  status: string;
}

export interface ProductImage {
  imageUrl: string;
  altText: string | null;
  sortOrder: number;
  isPrimary: boolean;
}

export interface ReviewSummary {
  averageRating: number;
  totalReviews: number;
  ratingDistribution: Record<number, number>;
}

export interface ProductDetail {
  id: string;
  name: string;
  slug: string;
  productType: string;
  status: string;
  shortDescription: string | null;
  description: string | null;
  material: string | null;
  brand: string | null;
  origin: string | null;
  careInstruction: string | null;
  categoryName: string;
  categorySlug: string;
  isFeatured: boolean;
  createdAt: string;
  updatedAt: string;
  variants: ProductVariant[];
  images: ProductImage[];
  reviewSummary: ReviewSummary | null;
}

export interface Review {
  id: string;
  userId: string;
  userFullName: string;
  userAvatarUrl: string | null;
  rating: number;
  comment: string | null;
  createdAt: string;
}

export interface Comment {
  id: string;
  userId: string;
  userFullName: string;
  userAvatarUrl: string | null;
  content: string;
  rating: number | null;
  parentCommentId: string | null;
  createdAt: string;
  replies: Comment[];
}

export type PaginatedReviews = PaginatedApiEnvelope<Review[]>;
export type PaginatedComments = PaginatedApiEnvelope<Comment[]>;
