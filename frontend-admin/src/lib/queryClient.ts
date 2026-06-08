import { QueryCache, QueryClient } from '@tanstack/react-query'
import { HttpError } from '@/api/client'

const ADMIN_QUERY_GC_TIME = 30 * 60_000

function isAuthBoundaryError(error: unknown): boolean {
  return error instanceof HttpError && (error.status === 401 || error.status === 403)
}

function handleAuthBoundaryError(error: unknown) {
  if (!isAuthBoundaryError(error)) return

  queryClient.clear()

  void import('@/stores/authStore').then(({ useAuthStore }) => {
    useAuthStore.getState().markAnonymous()
  })
}

export const queryClient = new QueryClient({
  queryCache: new QueryCache({
    onError: handleAuthBoundaryError,
  }),
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      gcTime: ADMIN_QUERY_GC_TIME,
      refetchOnWindowFocus: false,
      retry: (failureCount, error) => {
        if (isAuthBoundaryError(error)) return false
        return failureCount < 2
      },
    },
  },
})

export function clearAdminQueryCache() {
  queryClient.clear()
}
