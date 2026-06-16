import { create } from 'zustand'
import type {
  AiMessage,
  AiSuggestion,
  AiPendingAction,
  AiChatRequest,
  SavedConversation,
  AdminConversationSummary,
  AdminConversationDetail,
  AdminConversationMessage,
  AiToolCall,
  AiToolResultMeta,
} from '@/types/ai'
import { AI_BLOG_DRAFT_STORAGE_KEY, type AiBlogDraft } from '@/types/blog'
import { request } from '@/api/client'

const API = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5043'
const STORAGE_KEY = 'admin-ai-conversations'

function logAiWarn(message: string, context?: unknown) {
  console.warn(`[AdminAI] ${message}`, context ?? '')
}

function genId() {
  return crypto.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(36).slice(2)}`
}

function parseToolResultMeta(raw?: string): AiToolResultMeta | undefined {
  if (!raw) return undefined

  try {
    const parsed = JSON.parse(raw)
    if (parsed?.meta) return parsed.meta as AiToolResultMeta

    if (typeof parsed?.result === 'string') {
      const nested = JSON.parse(parsed.result)
      return nested?.meta as AiToolResultMeta | undefined
    }

    return parsed?.result?.meta as AiToolResultMeta | undefined
  } catch (err) {
    logAiWarn('Không đọc được metadata kết quả tool', err)
    return undefined
  }
}

function parseBlogDraftPayload(raw?: string): AiBlogDraft | undefined {
  if (!raw) return undefined

  try {
    const parsed = JSON.parse(raw)
    const data = parsed?.data ?? parsed?.result?.data ?? parsed?.result ?? parsed
    const draft = data?.kind === 'blog_draft' ? data.draft : data?.draft
    if (!draft || typeof draft.title !== 'string' || !Array.isArray(draft.content)) return undefined
    return draft as AiBlogDraft
  } catch (err) {
    logAiWarn('Không đọc được bản nháp blog từ AI', err)
    return undefined
  }
}

function persistBlogDraftHandoff(draft: AiBlogDraft) {
  try {
    sessionStorage.setItem(AI_BLOG_DRAFT_STORAGE_KEY, JSON.stringify(draft))
  } catch (err) {
    logAiWarn('Không lưu được bản nháp blog AI', err)
  }
}

function makeToolCall(toolName: string, input: string): AiToolCall {
  return {
    toolName,
    input,
  }
}

function applyToolResult(toolCall: AiToolCall, result: string): AiToolCall {
  const blogDraft = toolCall.toolName === 'generate_blog_draft' ? parseBlogDraftPayload(result) : undefined

  return {
    ...toolCall,
    result,
    meta: parseToolResultMeta(result),
    blogDraft,
  }
}

// --- localStorage helpers ---

function loadConversations(): SavedConversation[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? JSON.parse(raw) : []
  } catch (err) {
    logAiWarn('Không đọc được lịch sử AI cục bộ', err)
    return []
  }
}

function persistConversations(convos: SavedConversation[]) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(convos))
  } catch (err) {
    logAiWarn('Không lưu được lịch sử AI cục bộ', err)
  }
}

function makeTitle(messages: AiMessage[]): string {
  const first = messages.find((m) => m.role === 'user')
  if (!first) return 'Cuộc trò chuyện mới'
  const text = first.content.trim()
  return text.length > 40 ? text.slice(0, 40) + '…' : text
}

function parseToolCalls(message: AdminConversationMessage): AiMessage['toolCalls'] {
  if (!message.toolCallsJson) return undefined
  try {
    const parsed = JSON.parse(message.toolCallsJson) as { name?: string; callId?: string }
    const toolName = parsed.name || parsed.callId || 'unknown'
    const base = makeToolCall(toolName, '')
    return [{ ...base, result: message.content, meta: parseToolResultMeta(message.content), blogDraft: toolName === 'generate_blog_draft' ? parseBlogDraftPayload(message.content) : undefined }]
  } catch (err) {
    logAiWarn('Không đọc được metadata tool call', err)
    return undefined
  }
}

function mapServerMessage(message: AdminConversationMessage): AiMessage | null {
  if (message.role === 'tool_response') return null
  return {
    id: genId(),
    role: message.role === 'user' ? 'user' : 'assistant',
    content: message.role === 'tool_call' ? '' : message.content,
    toolCalls: message.role === 'tool_call' ? parseToolCalls(message) : undefined,
    createdAt: message.createdAt,
  }
}

function mapServerConversation(summary: AdminConversationSummary): SavedConversation {
  return {
    id: summary.id,
    conversationId: summary.id,
    title: summary.title || 'Cuộc trò chuyện mới',
    messages: [],
    createdAt: summary.updatedAt,
    updatedAt: summary.updatedAt,
  }
}

function mapConversationDetail(detail: AdminConversationDetail): SavedConversation {
  return {
    id: detail.id,
    conversationId: detail.id,
    title: detail.title || 'Cuộc trò chuyện mới',
    messages: detail.messages.map(mapServerMessage).filter((m): m is AiMessage => Boolean(m)),
    createdAt: detail.createdAt,
    updatedAt: detail.updatedAt,
  }
}

// --- State interface ---

interface AdminAiState {
  isOpen: boolean
  messages: AiMessage[]
  isLoading: boolean
  lastError: string | null
  conversationId: string | null
  pendingActions: AiPendingAction[]
  suggestions: AiSuggestion[]

  // Chat history
  conversations: SavedConversation[]
  activeConversationId: string | null

  toggle: () => void
  open: () => void
  close: () => void
  sendMessage: (req: AiChatRequest) => Promise<void>
  confirmAction: (actionId: string, approved: boolean) => Promise<boolean>
  continueAfterConfirm: (conversationId: string) => Promise<void>
  fetchSuggestions: () => Promise<void>
  clearConversation: () => void

  // Chat history actions
  fetchConversations: () => Promise<void>
  saveCurrentConversation: () => void
  loadConversation: (id: string) => Promise<void>
  deleteConversation: (id: string) => Promise<void>
  newConversation: () => void
}

export const useAdminAiStore = create<AdminAiState>((set, get) => ({
  isOpen: false,
  messages: [],
  isLoading: false,
  lastError: null,
  conversationId: null,
  pendingActions: [],
  suggestions: [],
  conversations: loadConversations(),
  activeConversationId: null,

  toggle: () => set((s) => ({ isOpen: !s.isOpen })),
  open: () => set({ isOpen: true }),
  close: () => set({ isOpen: false }),

  sendMessage: async (req: AiChatRequest) => {
    const state = get()
    if (state.isLoading) return
    if (!req.message.trim()) return

    const userMsg: AiMessage = {
      id: genId(),
      role: 'user',
      content: req.message,
      createdAt: new Date().toISOString(),
    }

    const assistantMsg: AiMessage = {
      id: genId(),
      role: 'assistant',
      content: '',
      toolCalls: [],
      createdAt: new Date().toISOString(),
    }

    set((s) => ({
      isLoading: true,
      lastError: null,
      messages: [...s.messages, userMsg, assistantMsg],
    }))

    try {
      const response = await fetch(`${API}/api/admin/ai/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({
          message: req.message,
          conversationId: req.conversationId || state.conversationId,
        }),
      })

      if (!response.ok) {
        const errText = await response.text().catch(() => 'Unknown error')
        throw new Error(`Server error: ${response.status} ${errText}`)
      }

      if (!response.body) throw new Error('No response body')

      const reader = response.body.getReader()
      const decoder = new TextDecoder()
      let buffer = ''
      let fullText = ''
      const toolCalls: AiMessage['toolCalls'] = []

      while (true) {
        const { done, value } = await reader.read()
        if (done) break

        buffer += decoder.decode(value, { stream: true })
        const lines = buffer.split('\n')
        buffer = lines.pop() || ''

        for (const line of lines) {
          if (!line.startsWith('data: ')) continue
          const data = line.slice(6).trim()
          if (!data || data === '[DONE]') continue

          try {
            const chunk = JSON.parse(data)

            if (chunk.type === 'text') {
              fullText += chunk.content
              set((s) => ({
                messages: s.messages.map((m) =>
                  m.id === assistantMsg.id ? { ...m, content: fullText } : m,
                ),
              }))
            } else if (chunk.type === 'tool_call') {
              toolCalls.push(makeToolCall(
                chunk.toolName || chunk.toolCallId || 'unknown',
                chunk.content,
              ))
              set((s) => ({
                messages: s.messages.map((m) =>
                  m.id === assistantMsg.id ? { ...m, toolCalls: [...toolCalls] } : m,
                ),
              }))
            } else if (chunk.type === 'tool_result') {
              const toolName = chunk.toolName || chunk.toolCallId || 'unknown'
              const existingIdx = toolCalls.findLastIndex((tc) => tc.toolName === toolName && !tc.result)
              const existing = existingIdx >= 0 ? toolCalls[existingIdx] : undefined
              const resolved = existing ? applyToolResult(existing, chunk.content) : { ...makeToolCall(toolName, chunk.content), result: chunk.content, meta: parseToolResultMeta(chunk.content) }
              if (existingIdx >= 0) toolCalls[existingIdx] = resolved
              else toolCalls.push(resolved)
              if (resolved.blogDraft) persistBlogDraftHandoff(resolved.blogDraft)
              set((s) => ({
                messages: s.messages.map((m) =>
                  m.id === assistantMsg.id ? { ...m, toolCalls: [...toolCalls] } : m,
                ),
              }))
            } else if (chunk.type === 'confirmation') {
              const pending: AiPendingAction = {
                actionId: chunk.toolCallId || genId(),
                toolName: chunk.toolName || 'unknown',
                description: chunk.content,
                riskLevel: 'medium',
                requestedAt: new Date().toISOString(),
                status: 'pending',
              }
              set((s) => ({
                pendingActions: [...s.pendingActions, pending],
                messages: s.messages.map((m) =>
                  m.id === assistantMsg.id
                    ? { ...m, pendingAction: pending }
                    : m,
                ),
              }))
            } else if (chunk.type === 'conversation') {
              set({ conversationId: chunk.content, activeConversationId: chunk.content })
            } else if (chunk.type === 'error') {
              set({ lastError: chunk.content || 'AI stream lỗi.' })
            }
          } catch (err) {
            logAiWarn('Bỏ qua SSE chunk lỗi', { length: data.length, err })
          }
        }
      }

      // Mark final
      set((s) => ({
        isLoading: false,
        lastError: null,
        messages: s.messages.map((m) =>
          m.id === assistantMsg.id
            ? { ...m, content: fullText || m.content, toolCalls: toolCalls }
            : m,
        ),
      }))
      get().saveCurrentConversation()
      await get().fetchConversations()
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Unknown error'
      set({ isLoading: false, lastError: errorMsg })
    }
  },

  confirmAction: async (actionId: string, approved: boolean) => {
    try {
      await request('/api/admin/ai/action/confirm', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ actionId, approved }),
      })
      set((s) => ({
        pendingActions: s.pendingActions.filter((a) => a.actionId !== actionId),
        messages: s.messages.map((m) =>
          m.pendingAction?.actionId === actionId
            ? { ...m, pendingAction: { ...m.pendingAction, status: approved ? 'confirmed' : 'rejected' } }
            : m
        ),
      }))
      get().saveCurrentConversation()
      return true
    } catch (err) {
      logAiWarn('Xác nhận AI action thất bại', { actionId, approved, err })
      set({ lastError: 'Không thể xác nhận hành động AI.' })
      return false
    }
  },

  /** Send a hidden continuation signal after confirming/rejecting a tool.
   *  Only adds an assistant bubble (no user bubble) since the tool result
   *  is already in the conversation history. */
  continueAfterConfirm: async (conversationId: string) => {
    const state = get()
    set({ isLoading: true, lastError: null })

    const assistantMsg: AiMessage = {
      id: genId(),
      role: 'assistant',
      content: '',
      toolCalls: [],
      createdAt: new Date().toISOString(),
    }

    set({ messages: [...state.messages, assistantMsg] })

    try {
      const response = await fetch(`${API}/api/admin/ai/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ message: '', conversationId }),
      })

      if (!response.ok || !response.body) throw new Error('No response')

      const reader = response.body.getReader()
      const decoder = new TextDecoder()
      let buffer = ''
      let fullText = ''
      const toolCalls: AiMessage['toolCalls'] = []

      while (true) {
        const { done, value } = await reader.read()
        if (done) break

        buffer += decoder.decode(value, { stream: true })
        const lines = buffer.split('\n')
        buffer = lines.pop() || ''

        for (const line of lines) {
          if (!line.startsWith('data: ')) continue
          const data = line.slice(6).trim()
          if (!data || data === '[DONE]') continue

          try {
            const chunk = JSON.parse(data)

            if (chunk.type === 'text') {
              fullText += chunk.content
              set((s) => ({
                messages: s.messages.map((m) =>
                  m.id === assistantMsg.id ? { ...m, content: fullText } : m,
                ),
              }))
            } else if (chunk.type === 'tool_call') {
              toolCalls.push(makeToolCall(
                chunk.toolName || chunk.toolCallId || 'unknown',
                chunk.content,
              ))
              set((s) => ({
                messages: s.messages.map((m) =>
                  m.id === assistantMsg.id ? { ...m, toolCalls: [...toolCalls] } : m,
                ),
              }))
            } else if (chunk.type === 'tool_result') {
              const toolName = chunk.toolName || chunk.toolCallId || 'unknown'
              const existingIdx = toolCalls.findLastIndex((tc) => tc.toolName === toolName && !tc.result)
              const existing = existingIdx >= 0 ? toolCalls[existingIdx] : undefined
              const resolved = existing ? applyToolResult(existing, chunk.content) : { ...makeToolCall(toolName, chunk.content), result: chunk.content, meta: parseToolResultMeta(chunk.content) }
              if (existingIdx >= 0) toolCalls[existingIdx] = resolved
              else toolCalls.push(resolved)
              if (resolved.blogDraft) persistBlogDraftHandoff(resolved.blogDraft)
              set((s) => ({
                messages: s.messages.map((m) =>
                  m.id === assistantMsg.id ? { ...m, toolCalls: [...toolCalls] } : m,
                ),
              }))
            } else if (chunk.type === 'confirmation') {
              const pending: AiPendingAction = {
                actionId: chunk.toolCallId || genId(),
                toolName: chunk.toolName || 'unknown',
                description: chunk.content,
                riskLevel: 'medium',
                requestedAt: new Date().toISOString(),
                status: 'pending',
              }
              set((s) => ({
                pendingActions: [...s.pendingActions, pending],
                messages: s.messages.map((m) =>
                  m.id === assistantMsg.id
                    ? { ...m, pendingAction: pending }
                    : m,
                ),
              }))
            } else if (chunk.type === 'conversation') {
              set({ conversationId: chunk.content, activeConversationId: chunk.content })
            } else if (chunk.type === 'error') {
              set({ lastError: chunk.content || 'AI stream lỗi.' })
            }
          } catch (err) {
            logAiWarn('Bỏ qua SSE chunk lỗi khi tiếp tục', { length: data.length, err })
          }
        }
      }

      set((s) => ({
        isLoading: false,
        messages: s.messages.map((m) =>
          m.id === assistantMsg.id
            ? { ...m, content: fullText || m.content, toolCalls }
            : m,
        ),
      }))
      get().saveCurrentConversation()
      await get().fetchConversations()
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Unknown error'
      logAiWarn('Tiếp tục AI sau xác nhận thất bại', err)
      set({ isLoading: false, lastError: errorMsg })
    }
  },

  fetchSuggestions: async () => {
    try {
      const data = await request<AiSuggestion[]>('/api/admin/ai/suggestions')
      set({ suggestions: data || [] })
    } catch (err) {
      logAiWarn('Không tải được gợi ý AI', err)
      set({ lastError: 'Không tải được gợi ý AI.' })
    }
  },

  clearConversation: () => {
    set({ messages: [], conversationId: null, pendingActions: [], activeConversationId: null })
  },

  // --- Chat history actions ---

  fetchConversations: async () => {
    try {
      const summaries = await request<AdminConversationSummary[]>('/api/admin/ai/conversations')
      const next = summaries.map(mapServerConversation)
      persistConversations(next)
      set({ conversations: next })
    } catch (err) {
      logAiWarn('Không tải được lịch sử AI từ máy chủ, dùng cache cục bộ', err)
      const fallback = loadConversations()
      set({ conversations: fallback, lastError: 'Không tải được lịch sử AI từ máy chủ.' })
    }
  },

  saveCurrentConversation: () => {
    const { messages, activeConversationId, conversations, conversationId } = get()
    if (messages.length === 0) return

    const now = new Date().toISOString()
    const id = conversationId || activeConversationId || genId()

    const existing = conversations.find((c) => c.id === id)
    const updated: SavedConversation = {
      id,
      conversationId: conversationId || id,
      title: existing?.title || makeTitle(messages),
      messages: messages.map((m) => ({
        ...m,
        content: m.content,
        toolCalls: m.toolCalls?.map((tc) => ({ toolName: tc.toolName, input: tc.input })),
        pendingAction: undefined,
      })),
      createdAt: existing?.createdAt || now,
      updatedAt: now,
    }

    const next = existing
      ? conversations.map((c) => (c.id === id ? updated : c))
      : [updated, ...conversations]

    persistConversations(next)
    set({ conversations: next, activeConversationId: id })
  },

  loadConversation: async (id: string) => {
    try {
      const detail = await request<AdminConversationDetail>(`/api/admin/ai/conversations/${id}`)
      const convo = mapConversationDetail(detail)
      const cached = get().conversations.filter((c) => c.id !== id)
      const next = [convo, ...cached]
      persistConversations(next)
      set({
        conversations: next,
        messages: convo.messages,
        activeConversationId: convo.id,
        conversationId: convo.conversationId ?? convo.id,
        pendingActions: [],
        lastError: null,
      })
    } catch (err) {
      logAiWarn('Không tải được cuộc trò chuyện AI từ máy chủ, dùng cache cục bộ', err)
      const convo = get().conversations.find((c) => c.id === id)
      if (!convo) {
        set({ lastError: 'Không tải được cuộc trò chuyện AI.' })
        return
      }
      set({
        messages: convo.messages,
        activeConversationId: id,
        conversationId: convo.conversationId ?? id,
        pendingActions: [],
        lastError: 'Đang hiển thị bản cache cục bộ.',
      })
    }
  },

  deleteConversation: async (id: string) => {
    try {
      await request(`/api/admin/ai/conversations/${id}`, { method: 'DELETE' })
    } catch (err) {
      logAiWarn('Không xóa được cuộc trò chuyện AI trên máy chủ, chỉ xóa cache cục bộ', err)
      set({ lastError: 'Không xóa được cuộc trò chuyện trên máy chủ.' })
    }

    const { conversations, activeConversationId } = get()
    const next = conversations.filter((c) => c.id !== id)
    persistConversations(next)
    set({
      conversations: next,
      ...(activeConversationId === id
        ? { activeConversationId: null, messages: [], conversationId: null, pendingActions: [] }
        : {}),
    })
  },

  newConversation: () => {
    set({
      messages: [],
      conversationId: null,
      pendingActions: [],
      activeConversationId: null,
    })
  },
}))
