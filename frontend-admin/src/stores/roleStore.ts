import { create } from 'zustand'
import type { RoleDto, CreateRoleRequest, UpdateRoleRequest } from '@/types/admin'
import * as adminApi from '@/api/admin'

interface RoleState {
  roles: RoleDto[]
  loading: boolean
  error: string | null
  fetchRoles: () => Promise<void>
  createRole: (data: CreateRoleRequest) => Promise<void>
  updateRole: (id: string, data: UpdateRoleRequest) => Promise<void>
  deleteRole: (id: string) => Promise<void>
  clearError: () => void
}

export const useRoleStore = create<RoleState>((set, get) => ({
  roles: [],
  loading: false,
  error: null,

  fetchRoles: async () => {
    set({ loading: true, error: null })
    try {
      const roles = await adminApi.getRoles()
      set({ roles, loading: false })
    } catch (err) {
      set({
        loading: false,
        error: err instanceof Error ? err.message : 'Không thể tải danh sách vai trò.',
      })
    }
  },

  createRole: async (data: CreateRoleRequest) => {
    set({ error: null })
    try {
      await adminApi.createRole(data)
      await get().fetchRoles()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể tạo vai trò.'
      set({ error: message })
      throw err
    }
  },

  updateRole: async (id: string, data: UpdateRoleRequest) => {
    set({ error: null })
    try {
      await adminApi.updateRole(id, data)
      await get().fetchRoles()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể cập nhật vai trò.'
      set({ error: message })
      throw err
    }
  },

  deleteRole: async (id: string) => {
    set({ error: null })
    try {
      await adminApi.deleteRole(id)
      await get().fetchRoles()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể xóa vai trò.'
      set({ error: message })
      throw err
    }
  },

  clearError: () => set({ error: null }),
}))
