<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# hooks/user

## Purpose
TanStack Query hooks for authenticated user account data. Query hooks fetch profile, addresses, and orders. Mutation hooks update profile, manage addresses (create/update/delete), and cancel orders — all with optimistic cache updates.

## Key Files
| File | Description |
|------|-------------|
| `useUserQueries.ts` | `useUserProfileQuery(enabled?)`, `useAddressesQuery(enabled?)`, `useOrdersQuery(enabled?)` — all gated by `enabled` (default true), stale times 5min/5min/2min respectively |
| `useUserMutations.ts` | `useUpdateProfileMutation()`, `useCreateAddressMutation()`, `useUpdateAddressMutation()`, `useDeleteAddressMutation()`, `useCancelOrderMutation()` |

## For AI Agents
### Working In This Directory
- All user/address/order queries are excluded from localStorage persistence.
- Address mutations update the cached `UserAddress[]` array directly: create appends, update replaces by id, delete filters by id.
- `useCancelOrderMutation` optimistically sets `orderStatus: 'cancelled'` on the matching order in the list cache.
- Pass `enabled={status === 'authenticated'}` to all queries to prevent unauthenticated fetches.

### Common Patterns
```ts
const { data: profile } = useUserProfileQuery(status === 'authenticated');
const { data: addresses } = useAddressesQuery(status === 'authenticated');
const updateProfile = useUpdateProfileMutation();
updateProfile.mutate({ fullName: 'Nguyễn Văn A', phone: '0912345678' });
```

## Dependencies
### Internal
- `src/api/user` — `getUserProfile`, `getAddresses`, `getOrders`, `updateProfile`, `createAddress`, `updateAddress`, `deleteAddress`, `cancelOrder`
- `src/lib/queryKeys` — `queryKeys.user.profile`, `queryKeys.addresses.list`, `queryKeys.orders.list`
- `src/types/address` — `UserAddress`, `CreateAddressPayload`
- `src/types/order` — `UserOrder`
- `src/types/user` — `UpdateProfilePayload`

### External
- `@tanstack/react-query` — `useQuery`, `useMutation`, `useQueryClient`

<!-- MANUAL: -->
