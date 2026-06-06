export interface AdminUserListItem {
  id: string
  fullName: string
  email: string | null
  phone: string | null
  status: string
  roles: string[]
  createdAt: string
  lastLoginAt: string | null
  isDeleted: boolean
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


// ── Products ──

export interface AdminProductListItem {
  id: string
  name: string
  slug: string
  productType: string
  categoryName: string
  status: string
  isFeatured: boolean
  variantCount: number
  isDeleted: boolean
  createdAt: string
}

export interface AdminVariantResponse {
  id: string
  sku: string
  variantName: string | null
  size: string | null
  color: string | null
  price: number
  salePrice: number | null
  stockQty: number
  isDefault: boolean
  status: string
}

export interface AdminImageResponse {
  id: string
  imageUrl: string
  altText: string | null
  sortOrder: number
  isPrimary: boolean
  isPublic: boolean
}

export interface AdminProductDetail {
  id: string
  name: string
  slug: string
  productType: string
  categoryId: string
  categoryName: string
  shortDescription: string | null
  description: string | null
  material: string | null
  brand: string | null
  origin: string | null
  careInstruction: string | null
  status: string
  isFeatured: boolean
  createdAt: string
  updatedAt: string
  variants: AdminVariantResponse[]
  images: AdminImageResponse[]
}

export interface CreateProductRequest {
  name: string
  slug: string
  productType: string
  categoryId: string
  shortDescription?: string
  description?: string
  material?: string
  brand?: string
  origin?: string
  careInstruction?: string
  status: string
  isFeatured: boolean
}

export interface UpdateProductRequest {
  name: string
  slug: string
  productType: string
  categoryId: string
  shortDescription?: string
  description?: string
  material?: string
  brand?: string
  origin?: string
  careInstruction?: string
  status: string
  isFeatured: boolean
}

// ── Categories ──

export interface CategoryListItem {
  id: string
  parent: string | null
  name: string
  slug: string
  description: string | null
  imageUrl: string | null
  sortOrder: number
  productCount: number
  isDeleted: boolean
  createdAt: string
}

export interface CategoryDetail {
  id: string
  parent: string | null
  name: string
  slug: string
  description: string | null
  imageUrl: string | null
  sortOrder: number
  createdAt: string
  updatedAt: string
}

export interface CreateCategoryRequest {
  name: string
  slug: string
  parent?: string | null
  description?: string
  imageUrl?: string
  sortOrder?: number
}

export interface UpdateCategoryRequest {
  name: string
  slug: string
  parent?: string | null
  description?: string
  imageUrl?: string
  sortOrder?: number
}

export interface CreateRoleRequest {
  name: string
  description?: string
}

export interface UpdateRoleRequest {
  name: string
  description?: string
}
