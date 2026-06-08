import { useQuery } from '@tanstack/react-query';
import * as authApi from '../../api/auth';
import { queryKeys } from '../../lib/queryKeys';
import type { AuthUser } from '../../types/auth';

export async function bootstrapCurrentUser(): Promise<AuthUser | null> {
  try {
    return await authApi.getCurrentUser();
  } catch {
    try {
      return await authApi.refreshSession();
    } catch {
      return null;
    }
  }
}

export function useCurrentUserQuery() {
  return useQuery({
    queryKey: queryKeys.auth.me,
    queryFn: bootstrapCurrentUser,
    staleTime: 5 * 60_000,
    gcTime: 30 * 60_000,
    retry: false,
  });
}
