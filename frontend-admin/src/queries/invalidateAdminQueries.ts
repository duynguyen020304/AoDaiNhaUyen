import { queryClient } from '@/lib/queryClient'
import { queryKeys } from './queryKeys'

export function invalidateAdminDashboardQueries() {
  void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.root })
}
