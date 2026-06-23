import { create } from 'zustand'
import * as api from '@/api/emailMarketing'
import type { EmailJobDetail, EmailJobListItem, EmailTemplateDetail, EmailTemplateListItem, ImportSubscribersRequest, MarketingCampaignSendResult, MarketingContentOption, MarketingStats, SendMarketingCampaignRequest, SubscriberDetail, SubscriberListItem } from '@/types/admin'

interface EmailMarketingState {
  stats: MarketingStats | null
  templates: EmailTemplateListItem[]
  selectedTemplate: EmailTemplateDetail | null
  subscribers: SubscriberListItem[]
  selectedSubscriber: SubscriberDetail | null
  contentOptions: MarketingContentOption[]
  jobs: EmailJobListItem[]
  selectedJob: EmailJobDetail | null
  totalPages: number
  totalItems: number
  currentPage: number
  pageSize: number
  loading: boolean
  error: string | null
  fetchStats: () => Promise<void>
  fetchContentOptions: () => Promise<void>
  sendCampaign: (data: SendMarketingCampaignRequest) => Promise<MarketingCampaignSendResult>
  fetchTemplates: (search?: string, page?: number) => Promise<void>
  fetchSubscribers: (search?: string, status?: string, page?: number) => Promise<void>
  loadSubscriber: (id: string) => Promise<void>
  unsubscribe: (id: string) => Promise<void>
  importSubscribers: (data: ImportSubscribersRequest) => Promise<void>
  fetchJobs: (status?: string, page?: number) => Promise<void>
  setPageSize: (pageSize: number) => void
  loadJob: (id: string) => Promise<void>
  retryJob: (id: string) => Promise<void>
  cancelJob: (id: string) => Promise<void>
  clearError: () => void
}

function message(error: unknown) { return error instanceof Error ? error.message : 'Đã xảy ra lỗi.' }

export const useEmailMarketingStore = create<EmailMarketingState>((set, get) => ({
  stats: null, templates: [], selectedTemplate: null, subscribers: [], selectedSubscriber: null, contentOptions: [], jobs: [], selectedJob: null,
  totalPages: 1, totalItems: 0, currentPage: 1, pageSize: 20, loading: false, error: null,
  clearError: () => set({ error: null }),
  fetchStats: async () => { set({ loading: true, error: null }); try { set({ stats: await api.getMarketingStats() }) } catch (e) { set({ error: message(e) }); throw e } finally { set({ loading: false }) } },
  fetchContentOptions: async () => { set({ error: null }); try { set({ contentOptions: await api.getMarketingContentOptions() }) } catch (e) { set({ error: message(e) }); throw e } },
  sendCampaign: async (data) => { set({ loading: true, error: null }); try { const result = await api.sendMarketingCampaign(data); await Promise.all([get().fetchStats(), get().fetchJobs('', 1)]); return result } catch (e) { set({ error: message(e) }); throw e } finally { set({ loading: false }) } },
  fetchTemplates: async (search = '', page = 1) => { set({ loading: true, error: null }); try { const r = await api.getEmailTemplates(search, false, page, get().pageSize); set({ templates: r.data, totalPages: r.totalPage, totalItems: r.totalItem, currentPage: page }) } catch (e) { set({ error: message(e) }); throw e } finally { set({ loading: false }) } },
  fetchSubscribers: async (search = '', status = '', page = 1) => { set({ loading: true, error: null }); try { const r = await api.getSubscribers(search, status, false, page, get().pageSize); set({ subscribers: r.data, totalPages: r.totalPage, totalItems: r.totalItem, currentPage: page }) } catch (e) { set({ error: message(e) }); throw e } finally { set({ loading: false }) } },
  loadSubscriber: async (id) => { set({ selectedSubscriber: await api.getSubscriber(id) }) },
  unsubscribe: async (id) => { await api.unsubscribeSubscriber(id); await get().fetchSubscribers('', '', get().currentPage) },
  importSubscribers: async (data) => { await api.importSubscribers(data); await get().fetchSubscribers() },
  fetchJobs: async (status = '', page = 1) => { set({ loading: true, error: null }); try { const r = await api.getEmailJobs(status, page, get().pageSize); set({ jobs: r.data, totalPages: r.totalPage, totalItems: r.totalItem, currentPage: page }) } catch (e) { set({ error: message(e) }); throw e } finally { set({ loading: false }) } },
  setPageSize: (pageSize) => set({ pageSize, currentPage: 1 }),
  loadJob: async (id) => { set({ selectedJob: await api.getEmailJob(id) }) },
  retryJob: async (id) => { await api.retryEmailJob(id); await get().fetchJobs('', get().currentPage) },
  cancelJob: async (id) => { await api.cancelEmailJob(id); await get().fetchJobs('', get().currentPage) },
}))
