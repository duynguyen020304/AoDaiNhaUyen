import { create } from 'zustand'
import { getLlmLog, getLlmLogs, getLlmLogStats } from '@/api/llmLogs'
import type { LlmAuditLogDetail, LlmAuditLogFilters, LlmAuditLogListItem, LlmAuditLogStats } from '@/types/llmLogs'

const initialFilters: LlmAuditLogFilters = {
  page: 1,
  pageSize: 20,
  sortBy: 'createdAt',
  sortDir: 'desc',
}

interface LlmLogState {
  items: LlmAuditLogListItem[]
  filters: LlmAuditLogFilters
  stats: LlmAuditLogStats | null
  selectedId: string | null
  selectedLog: LlmAuditLogDetail | null
  totalItem: number
  totalPage: number
  hasNextPage: boolean
  hasPreviousPage: boolean
  loadingList: boolean
  loadingDetail: boolean
  error: string | null
  setFilters: (filters: Partial<LlmAuditLogFilters>) => void
  resetFilters: () => void
  fetchLogs: () => Promise<void>
  fetchStats: () => Promise<void>
  openDetail: (id: string) => Promise<void>
  closeDetail: () => void
}

export const useLlmLogStore = create<LlmLogState>((set, get) => ({
  items: [],
  filters: initialFilters,
  stats: null,
  selectedId: null,
  selectedLog: null,
  totalItem: 0,
  totalPage: 1,
  hasNextPage: false,
  hasPreviousPage: false,
  loadingList: false,
  loadingDetail: false,
  error: null,

  setFilters: (filters) => set((state) => ({ filters: { ...state.filters, ...filters, page: filters.page ?? 1 } })),
  resetFilters: () => set({ filters: initialFilters }),

  fetchLogs: async () => {
    set({ loadingList: true, error: null })
    try {
      const response = await getLlmLogs(get().filters)
      set({
        items: response.data,
        totalItem: response.totalItem,
        totalPage: response.totalPage,
        hasNextPage: response.hasNextPage,
        hasPreviousPage: response.hasPreviousPage,
      })
    } catch (err) {
      set({ items: [], error: err instanceof Error ? err.message : 'Không thể tải nhật ký LLM.' })
    } finally {
      set({ loadingList: false })
    }
  },

  fetchStats: async () => {
    try {
      const stats = await getLlmLogStats(get().filters)
      set({ stats })
    } catch (err) {
      console.warn('[LlmLogs] Không thể tải thống kê', err)
    }
  },

  openDetail: async (id) => {
    set({ selectedId: id, selectedLog: null, loadingDetail: true, error: null })
    try {
      const detail = await getLlmLog(id)
      if (get().selectedId === id) set({ selectedLog: detail })
    } catch (err) {
      set({ error: err instanceof Error ? err.message : 'Không thể tải chi tiết nhật ký LLM.' })
    } finally {
      set({ loadingDetail: false })
    }
  },

  closeDetail: () => set({ selectedId: null, selectedLog: null, loadingDetail: false }),
}))
