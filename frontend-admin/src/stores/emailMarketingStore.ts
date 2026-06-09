import { create } from 'zustand'
import * as api from '@/api/emailMarketing'
import type { CreateEmailTemplateRequest, EmailJobDetail, EmailJobListItem, EmailTemplateDetail, EmailTemplateListItem, ImportSubscribersRequest, MarketingStats, SubscriberDetail, SubscriberListItem, UpdateEmailTemplateRequest } from '@/types/admin'

interface EmailMarketingState {
  stats: MarketingStats | null
  templates: EmailTemplateListItem[]
  selectedTemplate: EmailTemplateDetail | null
  subscribers: SubscriberListItem[]
  selectedSubscriber: SubscriberDetail | null
  jobs: EmailJobListItem[]
  selectedJob: EmailJobDetail | null
  totalPages: number
  currentPage: number
  loading: boolean
  error: string | null
  fetchStats: () => Promise<void>
  fetchTemplates: (search?: string, page?: number) => Promise<void>
  saveTemplate: (data: CreateEmailTemplateRequest | UpdateEmailTemplateRequest, id?: string) => Promise<void>
  deleteTemplate: (id: string) => Promise<void>
  restoreTemplate: (id: string) => Promise<void>
  fetchSubscribers: (search?: string, status?: string, page?: number) => Promise<void>
  loadSubscriber: (id: string) => Promise<void>
  unsubscribe: (id: string) => Promise<void>
  importSubscribers: (data: ImportSubscribersRequest) => Promise<void>
  fetchJobs: (status?: string, page?: number) => Promise<void>
  loadJob: (id: string) => Promise<void>
  retryJob: (id: string) => Promise<void>
  cancelJob: (id: string) => Promise<void>
  clearError: () => void
}

function message(error: unknown) { return error instanceof Error ? error.message : 'Đã xảy ra lỗi.' }

export const useEmailMarketingStore = create<EmailMarketingState>((set, get) => ({
  stats: null, templates: [], selectedTemplate: null, subscribers: [], selectedSubscriber: null, jobs: [], selectedJob: null,
  totalPages: 1, currentPage: 1, loading: false, error: null,
  clearError: () => set({ error: null }),
  fetchStats: async () => { set({ loading: true, error: null }); try { set({ stats: await api.getMarketingStats() }) } catch (e) { set({ error: message(e) }); throw e } finally { set({ loading: false }) } },
  fetchTemplates: async (search = '', page = 1) => { set({ loading: true, error: null }); try { const r = await api.getEmailTemplates(search, true, page); set({ templates: r.data, totalPages: r.totalPage, currentPage: page }) } catch (e) { set({ error: message(e) }); throw e } finally { set({ loading: false }) } },
  saveTemplate: async (data, id) => { set({ error: null }); try { if (id) { await api.updateEmailTemplate(id, data as UpdateEmailTemplateRequest) } else { await api.createEmailTemplate(data as CreateEmailTemplateRequest) } await get().fetchTemplates('', get().currentPage) } catch (e) { set({ error: message(e) }); throw e } },
  deleteTemplate: async (id) => { await api.deleteEmailTemplate(id); await get().fetchTemplates('', get().currentPage) },
  restoreTemplate: async (id) => { await api.restoreEmailTemplate(id); await get().fetchTemplates('', get().currentPage) },
  fetchSubscribers: async (search = '', status = '', page = 1) => { set({ loading: true, error: null }); try { const r = await api.getSubscribers(search, status, false, page); set({ subscribers: r.data, totalPages: r.totalPage, currentPage: page }) } catch (e) { set({ error: message(e) }); throw e } finally { set({ loading: false }) } },
  loadSubscriber: async (id) => { set({ selectedSubscriber: await api.getSubscriber(id) }) },
  unsubscribe: async (id) => { await api.unsubscribeSubscriber(id); await get().fetchSubscribers('', '', get().currentPage) },
  importSubscribers: async (data) => { await api.importSubscribers(data); await get().fetchSubscribers() },
  fetchJobs: async (status = '', page = 1) => { set({ loading: true, error: null }); try { const r = await api.getEmailJobs(status, page); set({ jobs: r.data, totalPages: r.totalPage, currentPage: page }) } catch (e) { set({ error: message(e) }); throw e } finally { set({ loading: false }) } },
  loadJob: async (id) => { set({ selectedJob: await api.getEmailJob(id) }) },
  retryJob: async (id) => { await api.retryEmailJob(id); await get().fetchJobs('', get().currentPage) },
  cancelJob: async (id) => { await api.cancelEmailJob(id); await get().fetchJobs('', get().currentPage) },
}))
