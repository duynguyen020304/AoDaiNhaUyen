import { create } from 'zustand'
import type { AdminUserListItem, RoleDto, CreateUserRequest, UpdateUserRequest } from '@/types/admin'
import * as adminApi from '@/api/admin'
import { invalidateAdminDashboardQueries } from '@/queries/invalidateAdminQueries'

interface UserState {
  users: AdminUserListItem[]
  roles: RoleDto[]
  totalPages: number
  totalItems: number
  currentPage: number
  pageSize: number
  search: string
  includeDeleted: boolean
  loading: boolean
  error: string | null
  fetchUsers: (search?: string, page?: number) => Promise<void>
  fetchRoles: () => Promise<void>
  createUser: (data: CreateUserRequest) => Promise<void>
  updateUser: (id: string, data: UpdateUserRequest) => Promise<void>
  updateUserRole: (id: string, roleId: string) => Promise<void>
  updateUserStatus: (id: string, status: string) => Promise<void>
  deleteUser: (id: string) => Promise<void>
  restoreUser: (id: string) => Promise<void>
  setSearch: (search: string) => void
  setIncludeDeleted: (value: boolean) => void
  setPageSize: (pageSize: number) => void
  clearError: () => void
}

export const useUserStore = create<UserState>((set, get) => ({
  users: [],
  roles: [],
  totalPages: 0,
  totalItems: 0,
  currentPage: 1,
  pageSize: 20,
  search: '',
  includeDeleted: false,
  loading: false,
  error: null,

  fetchUsers: async (search?: string, page?: number) => {
    const s = search ?? get().search
    const p = page ?? get().currentPage
    set({ loading: true, error: null })
    try {
      const result = await adminApi.getUsers(s || undefined, p, get().pageSize, get().includeDeleted)
      set({
        users: result.data,
        totalPages: result.totalPage,
        totalItems: result.totalItem,
        currentPage: p,
        search: s,
        loading: false,
      })
    } catch (err) {
      set({
        loading: false,
        error: err instanceof Error ? err.message : 'Không thể tải danh sách người dùng.',
      })
    }
  },

  fetchRoles: async () => {
    try {
      const roles = await adminApi.getRoles()
      set({ roles })
    } catch {
      // roles fetch failure is non-blocking
    }
  },

  createUser: async (data: CreateUserRequest) => {
    set({ error: null })
    try {
      await adminApi.createUser(data)
      await get().fetchUsers()
      invalidateAdminDashboardQueries()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể tạo người dùng.'
      set({ error: message })
      throw err
    }
  },

  updateUser: async (id: string, data: UpdateUserRequest) => {
    set({ error: null })
    try {
      await adminApi.updateUser(id, data)
      await get().fetchUsers()
      invalidateAdminDashboardQueries()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể cập nhật người dùng.'
      set({ error: message })
      throw err
    }
  },

  updateUserRole: async (id: string, roleId: string) => {
    try {
      await adminApi.updateUserRole(id, { roleId })
      await get().fetchUsers()
      invalidateAdminDashboardQueries()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể cập nhật vai trò.'
      set({ error: message })
      throw err
    }
  },

  updateUserStatus: async (id: string, status: string) => {
    try {
      await adminApi.updateUserStatus(id, { status })
      await get().fetchUsers()
      invalidateAdminDashboardQueries()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể cập nhật trạng thái.'
      set({ error: message })
      throw err
    }
  },

  deleteUser: async (id: string) => {
    set({ error: null })
    try {
      await adminApi.deleteUser(id)
      await get().fetchUsers()
      invalidateAdminDashboardQueries()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể xóa người dùng.'
      set({ error: message })
      throw err
    }
  },

  restoreUser: async (id: string) => {
    set({ error: null })
    try {
      await adminApi.restoreUser(id)
      await get().fetchUsers()
      invalidateAdminDashboardQueries()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Không thể khôi phục người dùng.'
      set({ error: message })
      throw err
    }
  },

  setSearch: (search: string) => set({ search }),
  setIncludeDeleted: (value: boolean) => set({ includeDeleted: value }),
  setPageSize: (pageSize: number) => set({ pageSize, currentPage: 1 }),
  clearError: () => set({ error: null }),
}))
