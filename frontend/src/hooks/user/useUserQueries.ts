import { useQuery } from '@tanstack/react-query';
import { getAddresses, getOrders, getUserProfile } from '../../api/user';
import { queryKeys } from '../../lib/queryKeys';

export function useUserProfileQuery(enabled = true) {
  return useQuery({
    queryKey: queryKeys.user.profile,
    queryFn: getUserProfile,
    enabled,
    staleTime: 5 * 60_000,
  });
}

export function useAddressesQuery(enabled = true) {
  return useQuery({
    queryKey: queryKeys.addresses.list,
    queryFn: getAddresses,
    enabled,
    staleTime: 5 * 60_000,
  });
}

export function useOrdersQuery(enabled = true) {
  return useQuery({
    queryKey: queryKeys.orders.list,
    queryFn: getOrders,
    enabled,
    staleTime: 2 * 60_000,
  });
}
