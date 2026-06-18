import { create } from 'zustand'
import { getHermesReport, getHermesReports } from '@/api/hermes'
import type { HermesReportDetail, HermesReportFilters, HermesReportListItem } from '@/types/hermes'

const initialFilters: HermesReportFilters = {
  page: 1,
  pageSize: 20,
}

let reportListRequestSeq = 0

interface HermesReportState {
  items: HermesReportListItem[]
  filters: HermesReportFilters
  selectedId: string | null
  selectedReport: HermesReportDetail | null
  totalItem: number
  totalPage: number
  hasNextPage: boolean
  hasPreviousPage: boolean
  loadingList: boolean
  loadingDetail: boolean
  error: string | null
  setFilters: (filters: Partial<HermesReportFilters>) => void
  resetFilters: () => void
  fetchReports: () => Promise<void>
  openDetail: (id: string) => Promise<void>
  closeDetail: () => void
}

export const useHermesReportStore = create<HermesReportState>((set, get) => ({
  items: [],
  filters: initialFilters,
  selectedId: null,
  selectedReport: null,
  totalItem: 0,
  totalPage: 1,
  hasNextPage: false,
  hasPreviousPage: false,
  loadingList: false,
  loadingDetail: false,
  error: null,

  setFilters: (filters) => set((state) => ({ filters: { ...state.filters, ...filters, page: filters.page ?? 1 } })),
  resetFilters: () => set({ filters: initialFilters }),

  fetchReports: async () => {
    const requestSeq = ++reportListRequestSeq
    const filters = get().filters
    set({ loadingList: true, error: null })
    try {
      const response = await getHermesReports(filters)
      if (requestSeq !== reportListRequestSeq) return
      set({
        items: response.data,
        totalItem: response.totalItem,
        totalPage: response.totalPage,
        hasNextPage: response.hasNextPage,
        hasPreviousPage: response.hasPreviousPage,
      })
    } catch (err) {
      if (requestSeq !== reportListRequestSeq) return
      set({ items: [], error: err instanceof Error ? err.message : 'Không thể tải báo cáo Hermes.' })
    } finally {
      if (requestSeq === reportListRequestSeq) set({ loadingList: false })
    }
  },

  openDetail: async (id) => {
    set({ selectedId: id, selectedReport: null, loadingDetail: true, error: null })
    try {
      const detail = await getHermesReport(id)
      if (get().selectedId === id) set({ selectedReport: detail })
    } catch (err) {
      if (get().selectedId === id) set({ error: err instanceof Error ? err.message : 'Không thể tải chi tiết báo cáo Hermes.' })
    } finally {
      if (get().selectedId === id) set({ loadingDetail: false })
    }
  },

  closeDetail: () => set({ selectedId: null, selectedReport: null, loadingDetail: false }),
}))
