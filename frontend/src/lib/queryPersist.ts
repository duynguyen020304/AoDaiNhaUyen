import { createSyncStoragePersister } from '@tanstack/query-sync-storage-persister';
import type { PersistedClient, Persister } from '@tanstack/react-query-persist-client';

const QUERY_CACHE_KEY = 'aodai.customer.query-cache';

const storage = window.localStorage;

export const queryPersister: Persister = createSyncStoragePersister({
  storage,
  key: QUERY_CACHE_KEY,
});

export async function clearPersistedQueryCache(): Promise<void> {
  storage.removeItem(QUERY_CACHE_KEY);
}

export function shouldDehydrateQuery(queryKey: readonly unknown[]): boolean {
  const [scope] = queryKey;
  return scope !== 'auth' && scope !== 'cart' && scope !== 'user' && scope !== 'orders' && scope !== 'addresses' && scope !== 'media';
}

export function pruneUnsafePersistedCache(client: PersistedClient): PersistedClient {
  return {
    ...client,
    clientState: {
      ...client.clientState,
      queries: client.clientState.queries.filter((query) => shouldDehydrateQuery(query.queryKey)),
    },
  };
}
