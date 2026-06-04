import { create } from 'zustand'
import type { AdminProductListItem, AdminProductDetail, CreateProductRequest, UpdateProductRequest } from '@/types/admin'
import * as adminApi from '@/api/admin'

interface ProductState {
  products: AdminProductListItem[]
  totalPages: number
  totalItems: number
  currentPage: number
  pageSize: number
  search: string
  statusFilter: string
  includeDeleted: boolean
  loading: boolean
  error: string | null
  fetchProducts: (search?: string, page?: number) => Promise<void>
  getProduct: (id: string) => Promise<AdminProductDetail>
  createProduct: (data: CreateProductRequest) => Promise<void>
  updateProduct: (id: string, data: UpdateProductRequest) => Promise<void>
  deleteProduct: (id: string) => Promise<void>
  restoreProduct: (id: string) => Promise<void>
  setSearch: (search: string) => void
  setStatusFilter: (status: string) => void
  setIncludeDeleted: (value: boolean) => void
  clearError: () => void
}

export const useProductStore = create<ProductState>((set, get) => ({
  products: [],
  totalPages: 0,
  totalItems: 0,
  currentPage: 1,
  pageSize: 20,
  search: '',
  statusFilter: '',
  includeDeleted: false,
  loading: false,
  error: null,

  fetchProducts: async (search?: string, page?: number) => {
    const s = search ?? get().search
    const p = page ?? get().currentPage
    set({ loading: true, error: null })
    try {
      const result = await adminApi.getProducts({
        search: s || undefined,
        status: get().statusFilter || undefined,
        page: p,
        pageSize: get().pageSize,
        includeDeleted: get().includeDeleted,
      })
      set({
        products: result.data,
        totalPages: result.totalPage,
        totalItems: result.totalItem,
        currentPage: p,
        search: s,
        loading: false,
      })
    } catch (err) {
      set({
        loading: false,
        error: err instanceof Error ? err.message : 'Không thể tải danh sách sản phẩm.',
      })
    }
  },

  getProduct: async (id: string) => {
    return adminApi.getProduct(id)
  },

  createProduct: async (data: CreateProductRequest) => {
    set({ error: null })
    try {
      await adminApi.createProduct(data)
      await get().fetchProducts()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể tạo sản phẩm.'
      set({ error: message })
      throw err
    }
  },

  updateProduct: async (id: string, data: UpdateProductRequest) => {
    set({ error: null })
    try {
      await adminApi.updateProduct(id, data)
      await get().fetchProducts()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể cập nhật sản phẩm.'
      set({ error: message })
      throw err
    }
  },

  deleteProduct: async (id: string) => {
    set({ error: null })
    try {
      await adminApi.deleteProduct(id)
      await get().fetchProducts()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể xóa sản phẩm.'
      set({ error: message })
      throw err
    }
  },

  restoreProduct: async (id: string) => {
    set({ error: null })
    try {
      await adminApi.restoreProduct(id)
      await get().fetchProducts()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể khôi phục sản phẩm.'
      set({ error: message })
      throw err
    }
  },

  setSearch: (search: string) => set({ search }),
  setStatusFilter: (status: string) => set({ statusFilter: status }),
  setIncludeDeleted: (value: boolean) => set({ includeDeleted: value }),
  clearError: () => set({ error: null }),
}))
