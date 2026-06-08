import { useMutation, useQueryClient } from '@tanstack/react-query';
import { cancelOrder, createAddress, deleteAddress, updateProfile } from '../../api/user';
import { queryKeys } from '../../lib/queryKeys';
import type { CreateAddressPayload, UserAddress } from '../../types/address';
import type { UserOrder } from '../../types/order';
import type { UpdateProfilePayload } from '../../types/user';

export function useUpdateProfileMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateProfilePayload) => updateProfile(payload),
    onSuccess: (profile) => queryClient.setQueryData(queryKeys.user.profile, profile),
    onSettled: () => void queryClient.invalidateQueries({ queryKey: queryKeys.user.profile }),
  });
}

export function useCreateAddressMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateAddressPayload) => createAddress(payload),
    onSuccess: (address) => queryClient.setQueryData<UserAddress[]>(queryKeys.addresses.list, (current = []) => [...current, address]),
    onSettled: () => void queryClient.invalidateQueries({ queryKey: queryKeys.addresses.list }),
  });
}

export function useDeleteAddressMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteAddress(id).then(() => id),
    onSuccess: (id) => queryClient.setQueryData<UserAddress[]>(queryKeys.addresses.list, (current = []) => current.filter((address) => address.id !== id)),
    onSettled: () => void queryClient.invalidateQueries({ queryKey: queryKeys.addresses.list }),
  });
}

export function useCancelOrderMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (orderId: string) => cancelOrder(orderId).then(() => orderId),
    onSuccess: (orderId) => queryClient.setQueryData<UserOrder[]>(queryKeys.orders.list, (current = []) => current.map((order) => order.id === orderId ? { ...order, orderStatus: 'cancelled' } : order)),
    onSettled: () => void queryClient.invalidateQueries({ queryKey: queryKeys.orders.list }),
  });
}
