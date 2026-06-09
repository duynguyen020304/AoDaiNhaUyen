import { create } from 'zustand'
import { createPromo, deletePromo, getPromo, getPromos, restorePromo, togglePromoStatus, updatePromo } from '@/api/admin'
import type { AdminPromoItem, CreatePromoRequest, UpdatePromoRequest } from '@/types/admin'

interface PromoState {
  promos: AdminPromoItem[]
  totalPages: number
  totalItems: number
  currentPage: number
  pageSize: number
  search: string
  activeFilter: string
  includeDeleted: boolean
  loading: boolean
  error: string | null
  fetchPromos: (opts?: { search?: string; page?: number; activeFilter?: string }) => Promise<void>
  getPromo: (id: string) => Promise<AdminPromoItem>
  createPromo: (data: CreatePromoRequest) => Promise<void>
  updatePromo: (id: string, data: UpdatePromoRequest) => Promise<void>
  deletePromo: (id: string) => Promise<void>
  restorePromo: (id: string) => Promise<void>
  togglePromoStatus: (id: string, isActive: boolean) => Promise<void>
  setSearch: (search: string) => void
  setActiveFilter: (activeFilter: string) => void
  setIncludeDeleted: (includeDeleted: boolean) => void
  clearError: () => void
}

function getErrorMessage(error: unknown, fallback: string) {
  return error instanceof Error ? error.message : fallback
}

function parseActiveFilter(value: string): boolean | undefined {
  if (value === 'active') return true
  if (value === 'inactive') return false
  return undefined
}

export const usePromoStore = create<PromoState>((set, get) => ({
  promos: [],
  totalPages: 0,
  totalItems: 0,
  currentPage: 1,
  pageSize: 20,
  search: '',
  activeFilter: '',
  includeDeleted: false,
  loading: false,
  error: null,

  fetchPromos: async (opts) => {
    const state = get()
    const search = opts?.search ?? state.search
    const activeFilter = opts?.activeFilter ?? state.activeFilter
    const page = opts?.page ?? state.currentPage

    set({ loading: true, error: null, search, activeFilter, currentPage: page })

    try {
      const current = get()
      const result = await getPromos({
        search,
        isActive: parseActiveFilter(activeFilter),
        includeDeleted: current.includeDeleted,
        page,
        pageSize: current.pageSize,
      })
      set({
        promos: result.data,
        totalPages: result.totalPage,
        totalItems: result.totalItem,
        loading: false,
      })
    } catch (error) {
      set({ error: getErrorMessage(error, 'Không thể tải danh sách mã giảm giá.'), loading: false })
      throw error
    }
  },

  getPromo: async (id) => getPromo(id),

  createPromo: async (data) => {
    set({ loading: true, error: null })
    try {
      await createPromo(data)
      await get().fetchPromos({ page: 1 })
    } catch (error) {
      set({ error: getErrorMessage(error, 'Không thể tạo mã giảm giá.'), loading: false })
      throw error
    }
  },

  updatePromo: async (id, data) => {
    set({ loading: true, error: null })
    try {
      await updatePromo(id, data)
      await get().fetchPromos()
    } catch (error) {
      set({ error: getErrorMessage(error, 'Không thể cập nhật mã giảm giá.'), loading: false })
      throw error
    }
  },

  deletePromo: async (id) => {
    set({ error: null })
    try {
      await deletePromo(id)
      await get().fetchPromos()
    } catch (error) {
      set({ error: getErrorMessage(error, 'Không thể xóa mã giảm giá.') })
      throw error
    }
  },

  restorePromo: async (id) => {
    set({ error: null })
    try {
      await restorePromo(id)
      await get().fetchPromos()
    } catch (error) {
      set({ error: getErrorMessage(error, 'Không thể khôi phục mã giảm giá.') })
      throw error
    }
  },

  togglePromoStatus: async (id, isActive) => {
    set({ error: null })
    try {
      await togglePromoStatus(id, isActive)
      await get().fetchPromos()
    } catch (error) {
      set({ error: getErrorMessage(error, 'Không thể cập nhật trạng thái mã giảm giá.') })
      throw error
    }
  },

  setSearch: (search) => set({ search }),
  setActiveFilter: (activeFilter) => set({ activeFilter }),
  setIncludeDeleted: (includeDeleted) => set({ includeDeleted }),
  clearError: () => set({ error: null }),
}))
