import { create } from 'zustand'
import * as blogApi from '@/api/blog'
import { BlogPayloadSchema, type BlogPost, type BlogPostListItem, type BlogPostPayload, type BlogStatus } from '@/types/blog'

interface BlogState {
  posts: BlogPostListItem[]
  selectedPost: BlogPost | null
  loading: boolean
  error: string | null
  page: number
  pageSize: number
  totalItem: number
  status: BlogStatus | ''
  search: string
  fetchPosts: () => Promise<void>
  fetchPost: (id: string) => Promise<BlogPost | null>
  createPost: (data: BlogPostPayload) => Promise<BlogPost>
  updatePost: (id: string, data: BlogPostPayload) => Promise<BlogPost>
  deletePost: (id: string) => Promise<void>
  setStatus: (status: BlogStatus | '') => void
  setSearch: (search: string) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: number) => void
  clearError: () => void
}

function parsePayload(data: BlogPostPayload): BlogPostPayload {
  const parsed = BlogPayloadSchema.safeParse(data)
  if (!parsed.success) {
    const flattened = zodFirstError(parsed.error)
    throw new Error(flattened || 'Dữ liệu bài viết không hợp lệ.')
  }
  return parsed.data as BlogPostPayload
}

function zodFirstError(error: { issues: { message: string }[] }) {
  return error.issues[0]?.message
}

export const useBlogStore = create<BlogState>((set, get) => ({
  posts: [],
  selectedPost: null,
  loading: false,
  error: null,
  page: 1,
  pageSize: 20,
  totalItem: 0,
  status: '',
  search: '',

  fetchPosts: async () => {
    const { status, search, page, pageSize } = get()
    set({ loading: true, error: null })
    try {
      const result = await blogApi.getBlogPosts({ status, search, page, pageSize })
      set({ posts: result.data, totalItem: result.totalItem, loading: false })
    } catch (err) {
      set({ loading: false, error: err instanceof Error ? err.message : 'Không thể tải bài viết.' })
    }
  },

  fetchPost: async (id) => {
    set({ loading: true, error: null })
    try {
      const post = await blogApi.getBlogPost(id)
      set({ selectedPost: post, loading: false })
      return post
    } catch (err) {
      set({ loading: false, error: err instanceof Error ? err.message : 'Không thể tải bài viết.' })
      return null
    }
  },

  createPost: async (data) => {
    set({ error: null })
    try {
      const post = await blogApi.createBlogPost(parsePayload(data))
      await get().fetchPosts()
      return post
    } catch (err) {
      set({ error: err instanceof Error ? err.message : 'Không thể tạo bài viết.' })
      throw err
    }
  },

  updatePost: async (id, data) => {
    set({ error: null })
    try {
      const post = await blogApi.updateBlogPost(id, parsePayload(data))
      await get().fetchPosts()
      set({ selectedPost: post })
      return post
    } catch (err) {
      set({ error: err instanceof Error ? err.message : 'Không thể cập nhật bài viết.' })
      throw err
    }
  },

  deletePost: async (id) => {
    set({ error: null })
    try {
      await blogApi.deleteBlogPost(id)
      await get().fetchPosts()
    } catch (err) {
      set({ error: err instanceof Error ? err.message : 'Không thể xóa bài viết.' })
      throw err
    }
  },

  setStatus: (status) => set({ status, page: 1 }),
  setSearch: (search) => set({ search, page: 1 }),
  setPage: (page) => set({ page }),
  setPageSize: (pageSize) => set({ pageSize, page: 1 }),
  clearError: () => set({ error: null }),
}))
