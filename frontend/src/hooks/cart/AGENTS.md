<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# hooks/cart

## Purpose
TanStack Query hooks for cart state. One query hook fetches the current cart; four mutation hooks add, update, remove, and clear cart items. All mutations normalize image asset URLs via `cartMapping.ts` after server responses.

## Key Files
| File | Description |
|------|-------------|
| `useCartQueries.ts` | `useCartQuery(enabled)` — fetches current cart, `staleTime: 0` (always refetch), `gcTime: 30min`. Calls `normalizeCartAssets()` on response |
| `useCartMutations.ts` | `useAddCartItemMutation()`, `useUpdateCartItemMutation()`, `useRemoveCartItemMutation()`, `useClearCartMutation()` — each sets query data optimistically in `onSuccess` and invalidates in `onSettled` |

## For AI Agents
### Working In This Directory
- Cart query is excluded from localStorage persistence (`shouldDehydrateQuery` blocks `scope === 'cart'`).
- `staleTime: 0` on the cart query means it always re-fetches on mount — intentional to keep cart fresh.
- `useClearCartMutation` uses `emptyCartFrom` helper to zero out items/totals locally without waiting for the server.
- `useCartQuery` is gated by `enabled` — pass `status === 'authenticated'` so it only runs when logged in.

### Common Patterns
```ts
const { data: cart } = useCartQuery(status === 'authenticated');
const addItem = useAddCartItemMutation();
addItem.mutate({ variantId: 'abc', quantity: 1 });
```

## Dependencies
### Internal
- `src/api/cart` — `getCart`, `addCartItem`, `updateCartItem`, `removeCartItem`, `clearCart`
- `src/lib/queryKeys` — `queryKeys.cart.current`
- `src/types/cart` — `Cart`, `AddCartItemPayload`, `UpdateCartItemPayload`
- `src/utils/cartMapping` — `normalizeCartAssets`, `emptyCartFrom`

### External
- `@tanstack/react-query` — `useQuery`, `useMutation`, `useQueryClient`

<!-- MANUAL: -->
