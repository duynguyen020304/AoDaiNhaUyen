import { useQuery } from '@tanstack/react-query';
import { getCart } from '../../api/cart';
import { queryKeys } from '../../lib/queryKeys';
import { normalizeCartAssets } from '../../utils/cartMapping';

export function useCartQuery(enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.cart.current,
    queryFn: async () => normalizeCartAssets(await getCart()),
    enabled,
    staleTime: 0,
    gcTime: 30 * 60_000,
  });
}
