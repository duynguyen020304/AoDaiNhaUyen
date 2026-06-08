import { create } from 'zustand'
import type { AdminProductListItem, AdminProductDetail, CreateProductRequest, UpdateProductRequest } from '@/types/admin'
import * as adminApi from '@/api/admin'
import { invalidateAdminDashboardQueries } from '@/queries/invalidateAdminQueries'

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
  updateVariantStock: (productId: string, variantId: string, stockQty: number) => Promise<AdminProductDetail>
  deleteProduct: (id: string) => Promise<void>
  restoreProduct: (id: string) => Promise<void>
  toggleProductStatus: (id: string, status: string) => Promise<void>
  uploadImage: (productId: string, file: File) => Promise<void>
  deleteImage: (productId: string, imageId: string) => Promise<void>
  setPrimaryImage: (productId: string, imageId: string) => Promise<void>
  toggleImageVisibility: (productId: string, imageId: string, isPublic: boolean) => Promise<void>
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
      invalidateAdminDashboardQueries()
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
      invalidateAdminDashboardQueries()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể cập nhật sản phẩm.'
      set({ error: message })
      throw err
    }
  },

  updateVariantStock: async (productId: string, variantId: string, stockQty: number) => {
    set({ error: null })
    try {
      const product = await adminApi.updateVariantStock(productId, variantId, { stockQty })
      await get().fetchProducts()
      invalidateAdminDashboardQueries()
      return product
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể cập nhật tồn kho.'
      set({ error: message })
      throw err
    }
  },

  deleteProduct: async (id: string) => {
    set({ error: null })
    try {
      await adminApi.deleteProduct(id)
      await get().fetchProducts()
      invalidateAdminDashboardQueries()
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
      invalidateAdminDashboardQueries()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể khôi phục sản phẩm.'
      set({ error: message })
      throw err
    }
  },

  toggleProductStatus: async (id: string, status: string) => {
    set({ error: null })
    try {
      await adminApi.toggleProductStatus(id, status)
      await get().fetchProducts()
      invalidateAdminDashboardQueries()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể cập nhật trạng thái.'
      set({ error: message })
      throw err
    }
  },

  uploadImage: async (productId: string, file: File) => {
    set({ error: null })
    try {
      await adminApi.uploadProductImage(productId, file)
      // re-fetch the product to update images
      await get().getProduct(productId)
      invalidateAdminDashboardQueries()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể tải ảnh lên.'
      set({ error: message })
      throw err
    }
  },

  deleteImage: async (productId: string, imageId: string) => {
    set({ error: null })
    try {
      await adminApi.deleteProductImage(productId, imageId)
      invalidateAdminDashboardQueries()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể xóa ảnh.'
      set({ error: message })
      throw err
    }
  },

  setPrimaryImage: async (productId: string, imageId: string) => {
    set({ error: null })
    try {
      await adminApi.setPrimaryProductImage(productId, imageId)
      invalidateAdminDashboardQueries()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể đặt ảnh chính.'
      set({ error: message })
      throw err
    }
  },

  toggleImageVisibility: async (productId: string, imageId: string, isPublic: boolean) => {
    set({ error: null })
    try {
      if (isPublic) {
        await adminApi.makeProductImagePublic(productId, imageId)
        invalidateAdminDashboardQueries()
      } else {
        await adminApi.makeProductImagePrivate(productId, imageId)
        invalidateAdminDashboardQueries()
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể đổi trạng thái ảnh.'
      set({ error: message })
      throw err
    }
  },

  setSearch: (search: string) => set({ search }),
  setStatusFilter: (status: string) => set({ statusFilter: status }),
  setIncludeDeleted: (value: boolean) => set({ includeDeleted: value }),
  clearError: () => set({ error: null }),
}))
