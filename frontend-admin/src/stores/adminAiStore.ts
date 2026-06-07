import { create } from 'zustand'
import type { AiMessage, AiSuggestion, AiPendingAction, AiChatRequest } from '@/types/ai'
import { request } from '@/api/client'

const API = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5043'

function genId() {
  return crypto.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(36).slice(2)}`
}

interface AdminAiState {
  isOpen: boolean
  messages: AiMessage[]
  isLoading: boolean
  conversationId: string | null
  pendingActions: AiPendingAction[]
  suggestions: AiSuggestion[]

  toggle: () => void
  open: () => void
  close: () => void
  sendMessage: (req: AiChatRequest) => Promise<void>
  confirmAction: (actionId: string, approved: boolean) => Promise<boolean>
  fetchSuggestions: () => Promise<void>
  clearConversation: () => void
}

export const useAdminAiStore = create<AdminAiState>((set, get) => ({
  isOpen: false,
  messages: [],
  isLoading: false,
  conversationId: null,
  pendingActions: [],
  suggestions: [],

  toggle: () => set((s) => ({ isOpen: !s.isOpen })),
  open: () => set({ isOpen: true }),
  close: () => set({ isOpen: false }),

  sendMessage: async (req: AiChatRequest) => {
    const state = get()
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

    set({
      isLoading: true,
      messages: [...state.messages, userMsg, assistantMsg],
    })

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
              toolCalls.push({
                toolName: chunk.toolName || chunk.toolCallId || 'unknown',
                input: chunk.content,
              })
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
              }
              set((s) => ({
                pendingActions: [...s.pendingActions, pending],
                messages: s.messages.map((m) =>
                  m.id === assistantMsg.id
                    ? { ...m, pendingAction: pending }
                    : m,
                ),
              }))
            } else if (chunk.type === 'error') {
              fullText += `\n❌ ${chunk.content}`
              set((s) => ({
                messages: s.messages.map((m) =>
                  m.id === assistantMsg.id ? { ...m, content: fullText } : m,
                ),
              }))
            }
          } catch {
            // skip malformed SSE
          }
        }
      }

      // Mark final
      set((s) => ({
        isLoading: false,
        conversationId: req.conversationId || state.conversationId,
        messages: s.messages.map((m) =>
          m.id === assistantMsg.id
            ? { ...m, content: fullText || m.content, toolCalls: toolCalls }
            : m,
        ),
      }))
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Unknown error'
      set((s) => ({
        isLoading: false,
        messages: s.messages.map((m) =>
          m.id === assistantMsg.id
            ? { ...m, content: `❌ Lỗi: ${errorMsg}` }
            : m,
        ),
      }))
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
      }))
      return true
    } catch {
      return false
    }
  },

  fetchSuggestions: async () => {
    try {
      const data = await request<AiSuggestion[]>('/api/admin/ai/suggestions')
      set({ suggestions: data || [] })
    } catch {
      // silent fail — suggestions are non-critical
    }
  },

  clearConversation: () => {
    set({ messages: [], conversationId: null, pendingActions: [] })
  },
}))
