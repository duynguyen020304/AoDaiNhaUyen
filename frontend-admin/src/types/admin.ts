export interface AdminUserListItem {
  id: string
  fullName: string
  email: string | null
  phone: string | null
  status: string
  roles: string[]
  createdAt: string
  lastLoginAt: string | null
}

export interface RoleDto {
  id: string
  name: string
  description: string | null
}

export interface CreateUserRequest {
  fullName: string
  email?: string
  phone?: string
  password?: string
  roleId?: string
}

export interface UpdateUserRequest {
  fullName: string
  email?: string
  phone?: string
}

export interface UpdateUserRoleRequest {
  roleId: string
}

export interface UpdateUserStatusRequest {
  status: string
}

export interface CreateRoleRequest {
  name: string
  description?: string
}

export interface UpdateRoleRequest {
  name: string
  description?: string
}
