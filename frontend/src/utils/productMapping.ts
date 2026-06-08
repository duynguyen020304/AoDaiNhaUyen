import { resolveAssetUrl } from '../api/client';
import type { ProductListItem } from '../types/catalog';
import type { Badge, Product } from '../pages/ProductsPage/data';

const vndFormatter = new Intl.NumberFormat('vi-VN', {
  style: 'currency',
  currency: 'VND',
  maximumFractionDigits: 0,
});

export function formatVnd(value: number) {
  return vndFormatter.format(value).replace('₫', 'đ');
}

export function getProductBadge(product: ProductListItem, index: number): Badge | undefined {
  if (product.isFeatured) {
    return index % 2 === 0 ? 'HOT' : 'BÁN CHẠY';
  }

  return product.status.toLowerCase() === 'active' && index < 2 ? 'MỚI' : undefined;
}

export function mapProductListItem(product: ProductListItem, index: number): Product {
  const price = product.salePrice ?? product.price;
  return {
    id: String(product.id),
    slug: product.slug,
    variantId: product.primaryVariantId,
    name: product.name,
    image: resolveAssetUrl(product.primaryImageUrl) ?? '/assets/products/product-truyen-thong-1.png',
    badge: getProductBadge(product, index),
    rating: product.averageRating,
    reviews: product.totalReviews,
    price: formatVnd(price),
    originalPrice: product.salePrice ? formatVnd(product.price) : undefined,
  };
}
