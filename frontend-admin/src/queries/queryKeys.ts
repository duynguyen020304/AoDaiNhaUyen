export type DashboardPeriod = 7 | 30 | 90

export const queryKeys = {
  auth: {
    root: ['auth'] as const,
    me: () => ['auth', 'me'] as const,
  },
  dashboard: {
    root: ['dashboard'] as const,
    summary: () => ['dashboard', 'summary'] as const,
    revenue: (period: DashboardPeriod) => ['dashboard', 'revenue', { period }] as const,
    ordersByStatus: () => ['dashboard', 'orders-by-status'] as const,
    recentOrders: (limit: number) => ['dashboard', 'recent-orders', { limit }] as const,
    topProducts: (limit: number) => ['dashboard', 'top-products', { limit }] as const,
    userGrowth: (period: DashboardPeriod) => ['dashboard', 'user-growth', { period }] as const,
  },
  products: {
    root: ['products'] as const,
  },
  blog: {
    root: ['blog'] as const,
    list: (params?: unknown) => ['blog', 'list', params] as const,
    detail: (id: string) => ['blog', 'detail', id] as const,
  },
  categories: {
    root: ['categories'] as const,
  },
  users: {
    root: ['users'] as const,
  },
  roles: {
    root: ['roles'] as const,
  },
  orders: {
    root: ['orders'] as const,
  },
  inventory: {
    root: ['inventory'] as const,
  },
  hermes: {
    root: ['hermes'] as const,
    events: (params?: unknown) => ['hermes', 'events', params] as const,
    monitor: (token: string) => ['hermes', 'monitor', token] as const,
  },
} as const
