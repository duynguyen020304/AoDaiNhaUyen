import { useQuery, useQueries, type UseQueryResult } from '@tanstack/react-query';
import { getHeaderCategories, getProductBySlug, getProducts, type GetProductsParams } from '../../api/catalog';
import type { HeaderCategoryChild, PaginatedProducts } from '../../types/catalog';
import { queryKeys } from '../../lib/queryKeys';

export function useHeaderCategoriesQuery() {
  return useQuery({
    queryKey: queryKeys.categories.header,
    queryFn: getHeaderCategories,
    staleTime: 60 * 60_000,
    gcTime: 24 * 60 * 60_000,
  });
}

export function useProductsQuery(params: GetProductsParams = {}, enabled = true) {
  return useQuery({
    queryKey: queryKeys.products.list(params),
    queryFn: () => getProducts(params),
    enabled,
    staleTime: 30_000,
    gcTime: 5 * 60_000,
  });
}

export function useProductDetailQuery(slug: string | undefined) {
  return useQuery({
    queryKey: queryKeys.products.detail(slug ?? ''),
    queryFn: () => getProductBySlug(slug ?? ''),
    enabled: Boolean(slug),
    staleTime: 5 * 60_000,
    gcTime: 30 * 60_000,
  });
}


export function useCategoryProductsQueries(
  categories: HeaderCategoryChild[],
  getParams: (category: HeaderCategoryChild) => GetProductsParams,
): UseQueryResult<PaginatedProducts>[] {
  return useQueries({
    queries: categories.map((category) => {
      const params = getParams(category);
      return {
        queryKey: queryKeys.products.list(params),
        queryFn: () => getProducts(params),
        staleTime: 30_000,
        gcTime: 5 * 60_000,
      };
    }),
  });
}
