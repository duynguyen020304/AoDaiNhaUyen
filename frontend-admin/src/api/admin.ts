import { request, requestPaginated } from './client'
import type { PaginatedApiEnvelope } from '@/types/api'
import type {
  AdminUserListItem,
  RoleDto,
  CreateUserRequest,
  UpdateUserRequest,
  UpdateUserRoleRequest,
  UpdateUserStatusRequest,
  CreateRoleRequest,
  UpdateRoleRequest,
} from '@/types/admin'

// ── Users ──

export async function getUsers(
  search?: string,
  page = 1,
  pageSize = 20,
): Promise<PaginatedApiEnvelope<AdminUserListItem[]>> {
  const params = new URLSearchParams()
  if (search) params.set('search', search)
  params.set('page', String(page))
  params.set('pageSize', String(pageSize))
  return requestPaginated<AdminUserListItem[]>(`/api/admin/users?${params}`)
}

export async function getUser(id: string): Promise<AdminUserListItem> {
  return request<AdminUserListItem>(`/api/admin/users/${id}`)
}

export async function createUser(data: CreateUserRequest): Promise<AdminUserListItem> {
  return request<AdminUserListItem>('/api/admin/users', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

export async function updateUser(id: string, data: UpdateUserRequest): Promise<AdminUserListItem> {
  return request<AdminUserListItem>(`/api/admin/users/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })
}

export async function updateUserRole(id: string, data: UpdateUserRoleRequest): Promise<void> {
  await request<void>(`/api/admin/users/${id}/role`, {
    method: 'PATCH',
    body: JSON.stringify(data),
  })
}

export async function updateUserStatus(id: string, data: UpdateUserStatusRequest): Promise<void> {
  await request<void>(`/api/admin/users/${id}/status`, {
    method: 'PATCH',
    body: JSON.stringify(data),
  })
}

export async function deleteUser(id: string): Promise<void> {
  await request<void>(`/api/admin/users/${id}`, { method: 'DELETE' })
}

// ── Roles ──

export async function getRoles(): Promise<RoleDto[]> {
  return request<RoleDto[]>('/api/admin/roles')
}

export async function createRole(data: CreateRoleRequest): Promise<RoleDto> {
  return request<RoleDto>('/api/admin/roles', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

export async function updateRole(id: string, data: UpdateRoleRequest): Promise<RoleDto> {
  return request<RoleDto>(`/api/admin/roles/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })
}

export async function deleteRole(id: string): Promise<void> {
  await request<void>(`/api/admin/roles/${id}`, { method: 'DELETE' })
}
