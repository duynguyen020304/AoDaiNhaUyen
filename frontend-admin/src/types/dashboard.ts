export interface DashboardSummary {
  totalRevenue: number
  totalOrders: number
  totalUsers: number
  totalProducts: number
  revenueGrowth: number
  ordersGrowth: number
  usersGrowth: number
  productsGrowth: number
}

export interface RevenuePoint {
  date: string
  revenue: number
  orders: number
}

export interface RevenueData {
  period: string
  points: RevenuePoint[]
}

export interface OrderStatusDistribution {
  pending: number
  confirmed: number
  processing: number
  shipping: number
  completed: number
  cancelled: number
  returned: number
}

export interface RecentOrder {
  id: string
  orderCode: string
  customerName: string
  totalAmount: number
  status: string
  createdAt: string
}

export interface TopProduct {
  productId: string | null
  productName: string
  imageUrl: string | null
  soldCount: number
  revenue: number
}

export interface UserGrowthPoint {
  date: string
  newUsers: number
  totalUsers: number
}

export interface UserGrowthData {
  points: UserGrowthPoint[]
}
