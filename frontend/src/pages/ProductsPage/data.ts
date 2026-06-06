export type Badge = 'HOT' | 'MỚI' | 'BÁN CHẠY';

export interface Product {
  id: string;
  slug: string;
  variantId: string | null;
  name: string;
  image: string;
  badge?: Badge;
  reviews: number;
  price: string;
  originalPrice?: string;
}

export interface Category {
  id: string;
  name: string;
  products: Product[];
}

export const SIZES = ['S', 'M', 'L', 'XL'] as const;

