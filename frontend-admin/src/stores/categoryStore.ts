import { create } from 'zustand'
import type { CategoryListItem, CreateCategoryRequest, UpdateCategoryRequest } from '@/types/admin'
import * as adminApi from '@/api/admin'

interface CategoryState {
  categories: CategoryListItem[]
  loading: boolean
  error: string | null
  includeDeleted: boolean
  fetchCategories: () => Promise<void>
  createCategory: (data: CreateCategoryRequest) => Promise<void>
  updateCategory: (id: string, data: UpdateCategoryRequest) => Promise<void>
  deleteCategory: (id: string) => Promise<void>
  restoreCategory: (id: string) => Promise<void>
  setIncludeDeleted: (value: boolean) => void
  clearError: () => void
}

export const useCategoryStore = create<CategoryState>((set, get) => ({
  categories: [],
  loading: false,
  error: null,
  includeDeleted: false,

  fetchCategories: async () => {
    set({ loading: true, error: null })
    try {
      const categories = await adminApi.getCategories(get().includeDeleted)
      set({ categories, loading: false })
    } catch (err) {
      set({
        loading: false,
        error: err instanceof Error ? err.message : 'Không thể tải danh mục.',
      })
    }
  },

  createCategory: async (data: CreateCategoryRequest) => {
    set({ error: null })
    try {
      await adminApi.createCategory(data)
      await get().fetchCategories()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể tạo danh mục.'
      set({ error: message })
      throw err
    }
  },

  updateCategory: async (id: string, data: UpdateCategoryRequest) => {
    set({ error: null })
    try {
      await adminApi.updateCategory(id, data)
      await get().fetchCategories()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể cập nhật danh mục.'
      set({ error: message })
      throw err
    }
  },

  deleteCategory: async (id: string) => {
    set({ error: null })
    try {
      await adminApi.deleteCategory(id)
      await get().fetchCategories()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể xóa danh mục.'
      set({ error: message })
      throw err
    }
  },

  restoreCategory: async (id: string) => {
    set({ error: null })
    try {
      await adminApi.restoreCategory(id)
      await get().fetchCategories()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể khôi phục danh mục.'
      set({ error: message })
      throw err
    }
  },

  setIncludeDeleted: (value: boolean) => set({ includeDeleted: value }),
  clearError: () => set({ error: null }),
}))
