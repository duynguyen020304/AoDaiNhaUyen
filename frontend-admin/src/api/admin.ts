import { request, requestPaginated } from './client'
import type { PaginatedApiEnvelope } from '@/types/api'
import type {
  AdminUserListItem,
  AdminProductListItem,
  AdminProductDetail,
  AdminImageResponse,
  CategoryListItem,
  CategoryDetail,
  RoleDto,
  CreateUserRequest,
  UpdateUserRequest,
  UpdateUserRoleRequest,
  UpdateUserStatusRequest,
  CreateProductRequest,
  UpdateProductRequest,
  CreateCategoryRequest,
  UpdateCategoryRequest,
  CreateRoleRequest,
  UpdateRoleRequest,
} from '@/types/admin'

// ── Users ──

export async function getUsers(
  search?: string,
  page = 1,
  pageSize = 20,
  includeDeleted = false,
): Promise<PaginatedApiEnvelope<AdminUserListItem[]>> {
  const params = new URLSearchParams()
  if (search) params.set('search', search)
  params.set('page', String(page))
  params.set('pageSize', String(pageSize))
  if (includeDeleted) params.set('includeDeleted', 'true')
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

export async function restoreUser(id: string): Promise<void> {
  await request<void>(`/api/admin/users/${id}/restore`, { method: 'PATCH' })
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
// ── Products ──

export async function getProducts(params?: {
  search?: string
  status?: string
  page?: number
  pageSize?: number
  includeDeleted?: boolean
}): Promise<PaginatedApiEnvelope<AdminProductListItem[]>> {
  const qs = new URLSearchParams()
  if (params?.search) qs.set('search', params.search)
  if (params?.status) qs.set('status', params.status)
  qs.set('page', String(params?.page ?? 1))
  qs.set('pageSize', String(params?.pageSize ?? 20))
  if (params?.includeDeleted) qs.set('includeDeleted', 'true')
  return requestPaginated<AdminProductListItem[]>(`/api/admin/products?${qs}`)
}

export async function getProduct(id: string): Promise<AdminProductDetail> {
  return request<AdminProductDetail>(`/api/admin/products/${id}`)
}

export async function createProduct(data: CreateProductRequest): Promise<AdminProductDetail> {
  return request<AdminProductDetail>('/api/admin/products', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

export async function updateProduct(id: string, data: UpdateProductRequest): Promise<AdminProductDetail> {
  return request<AdminProductDetail>(`/api/admin/products/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })
}

export async function toggleProductStatus(id: string, status: string): Promise<void> {
  await request<void>(`/api/admin/products/${id}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ status }),
  })
}

export async function deleteProduct(id: string): Promise<void> {
  await request<void>(`/api/admin/products/${id}`, { method: 'DELETE' })
}

export async function restoreProduct(id: string): Promise<void> {
  await request<void>(`/api/admin/products/${id}/restore`, { method: 'PATCH' })
}

export async function uploadProductImage(productId: string, file: File): Promise<AdminImageResponse> {
  const formData = new FormData()
  formData.append('file', file)
  return request<AdminImageResponse>(`/api/admin/products/${productId}/images`, {
    method: 'POST',
    body: formData,
  })
}

export async function deleteProductImage(productId: string, imageId: string): Promise<void> {
  await request<void>(`/api/admin/products/${productId}/images/${imageId}`, { method: 'DELETE' })
}

export async function setPrimaryProductImage(productId: string, imageId: string): Promise<void> {
  await request<void>(`/api/admin/products/${productId}/images/${imageId}/primary`, { method: 'PUT' })
}

export async function makeProductImagePublic(productId: string, imageId: string): Promise<void> {
  await request<void>(`/api/admin/products/${productId}/images/${imageId}/make-public`, { method: 'POST' })
}

export async function makeProductImagePrivate(productId: string, imageId: string): Promise<void> {
  await request<void>(`/api/admin/products/${productId}/images/${imageId}/make-private`, { method: 'POST' })
}

// ── Categories ──

export async function getCategories(includeDeleted = false): Promise<CategoryListItem[]> {
  const qs = new URLSearchParams()
  if (includeDeleted) qs.set('includeDeleted', 'true')
  return request<CategoryListItem[]>(`/api/admin/categories?${qs}`)
}

export async function getCategory(id: string): Promise<CategoryDetail> {
  return request<CategoryDetail>(`/api/admin/categories/${id}`)
}

export async function createCategory(data: CreateCategoryRequest): Promise<CategoryDetail> {
  return request<CategoryDetail>('/api/admin/categories', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

export async function updateCategory(id: string, data: UpdateCategoryRequest): Promise<CategoryDetail> {
  return request<CategoryDetail>(`/api/admin/categories/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })
}

export async function deleteCategory(id: string): Promise<void> {
  await request<void>(`/api/admin/categories/${id}`, { method: 'DELETE' })
}

export async function restoreCategory(id: string): Promise<void> {
  await request<void>(`/api/admin/categories/${id}/restore`, { method: 'PATCH' })
}

