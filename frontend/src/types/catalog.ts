export interface HeaderCategoryChild {
  id: number;
  name: string;
  slug: string;
  sortOrder: number;
}

export interface HeaderCategory {
  id: number;
  name: string;
  slug: string;
  sortOrder: number;
  children: HeaderCategoryChild[];
}

export interface ProductListItem {
  id: number;
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
  primaryVariantSku: string | null;
}

export interface PaginatedProducts {
  items: ProductListItem[];
  meta: {
    total: number;
    page: number;
    pageSize: number;
    totalPages: number;
  };
}
