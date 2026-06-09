import { useQuery } from '@tanstack/react-query';
import { getProductsBySlugs } from '../../api/catalog';

export function useProductSpotlight(slugs: string[]) {
  const normalized = slugs.filter(Boolean).slice(0, 12);
  return useQuery({
    queryKey: ['products', 'spotlight', normalized],
    queryFn: () => getProductsBySlugs(normalized),
    enabled: normalized.length > 0,
    staleTime: 5 * 60_000,
    gcTime: 30 * 60_000,
  });
}
