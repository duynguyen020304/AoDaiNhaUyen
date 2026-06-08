import { useMutation, useQueryClient } from '@tanstack/react-query';
import { addCartItem, clearCart, removeCartItem, updateCartItem } from '../../api/cart';
import { queryKeys } from '../../lib/queryKeys';
import type { AddCartItemPayload, Cart, UpdateCartItemPayload } from '../../types/cart';
import { emptyCartFrom, normalizeCartAssets } from '../../utils/cartMapping';

export function useAddCartItemMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: AddCartItemPayload) => addCartItem(payload),
    onSuccess: (cart) => queryClient.setQueryData(queryKeys.cart.current, normalizeCartAssets(cart)),
    onSettled: () => void queryClient.invalidateQueries({ queryKey: queryKeys.cart.current }),
  });
}

export function useUpdateCartItemMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ itemId, payload }: { itemId: string; payload: UpdateCartItemPayload }) => updateCartItem(itemId, payload),
    onSuccess: (cart) => queryClient.setQueryData(queryKeys.cart.current, normalizeCartAssets(cart)),
    onSettled: () => void queryClient.invalidateQueries({ queryKey: queryKeys.cart.current }),
  });
}

export function useRemoveCartItemMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (itemId: string) => removeCartItem(itemId),
    onSuccess: (cart) => queryClient.setQueryData(queryKeys.cart.current, normalizeCartAssets(cart)),
    onSettled: () => void queryClient.invalidateQueries({ queryKey: queryKeys.cart.current }),
  });
}

export function useClearCartMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: clearCart,
    onSuccess: () => queryClient.setQueryData<Cart | undefined>(queryKeys.cart.current, emptyCartFrom),
    onSettled: () => void queryClient.invalidateQueries({ queryKey: queryKeys.cart.current }),
  });
}
