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
  AiGeneratedImagePreview,
  AiBlogClarification,
  AdminChatMode,
  HermesStatus,
} from '@/types/ai'
import { AI_BLOG_DRAFT_STORAGE_KEY, type AiBlogDraft, type AiGeneratedImageAsset, type AiImagePlan, type AiPhaseStatus } from '@/types/blog'
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
    const generatedImages = parseBlogImageAssets(raw)
    const featured = generatedImages?.find((image) => image.kind === 'featured')
    return {
      ...(draft as AiBlogDraft),
      selectedTemplate: typeof data?.selectedTemplate === 'string' ? data.selectedTemplate : undefined,
      templateReason: typeof data?.templateReason === 'string' ? data.templateReason : undefined,
      phases: parsePhaseStatusPayload(raw),
      imagePlan: parseImagePlanPayload(raw),
      generatedImages,
      featuredImage: featured?.objectKey ?? draft.featuredImage ?? null,
      featuredImageWidth: featured?.width ?? draft.featuredImageWidth ?? 1200,
      featuredImageHeight: featured?.height ?? draft.featuredImageHeight ?? 630,
      qualityWarnings: parseWarnings(raw) ?? draft.qualityWarnings,
    } satisfies AiBlogDraft
  } catch (err) {
    logAiWarn('Không đọc được bản nháp blog từ AI', err)
    return undefined
  }
}

function parseBlogClarificationPayload(raw?: string): AiBlogClarification | undefined {
  if (!raw) return undefined

  try {
    const parsed = JSON.parse(raw)
    const data = parsed?.data ?? parsed?.result?.data ?? parsed?.result ?? parsed
    if (data?.kind !== 'blog_draft_clarification') return undefined
    if (!Array.isArray(data.questions)) return undefined
    return {
      selectedTemplate: typeof data.selectedTemplate === 'string' ? data.selectedTemplate : undefined,
      templateReason: typeof data.templateReason === 'string' ? data.templateReason : undefined,
      questions: data.questions.filter((x: unknown): x is string => typeof x === 'string'),
      suggestedAnswers: Array.isArray(data.suggestedAnswers) ? data.suggestedAnswers.filter((x: unknown): x is string => typeof x === 'string') : undefined,
      phases: Array.isArray(data.phases) ? data.phases as AiPhaseStatus[] : undefined,
      warnings: Array.isArray(data.warnings) ? data.warnings.filter((x: unknown): x is string => typeof x === 'string') : undefined,
    }
  } catch (err) {
    logAiWarn('Không đọc được yêu cầu bổ sung blog từ AI', err)
    return undefined
  }
}

function parseImagePlanPayload(raw?: string): AiImagePlan | undefined {
  if (!raw) return undefined
  try {
    const parsed = JSON.parse(raw)
    const data = parsed?.data ?? parsed?.result?.data ?? parsed?.result ?? parsed
    const plan = data?.imagePlan
    if (!plan || typeof plan !== 'object') return undefined
    return plan as AiImagePlan
  } catch (err) {
    logAiWarn('Không đọc được image plan từ AI', err)
    return undefined
  }
}

function parsePhaseStatusPayload(raw?: string): AiPhaseStatus[] | undefined {
  if (!raw) return undefined
  try {
    const parsed = JSON.parse(raw)
    const data = parsed?.data ?? parsed?.result?.data ?? parsed?.result ?? parsed
    return Array.isArray(data?.phases) ? data.phases as AiPhaseStatus[] : undefined
  } catch (err) {
    logAiWarn('Không đọc được phase trạng thái blog từ AI', err)
    return undefined
  }
}

function parseBlogImageAssets(raw?: string): AiGeneratedImageAsset[] | undefined {
  if (!raw) return undefined
  try {
    const parsed = JSON.parse(raw)
    const data = parsed?.data ?? parsed?.result?.data ?? parsed?.result ?? parsed
    return Array.isArray(data?.generatedImages) ? data.generatedImages as AiGeneratedImageAsset[] : undefined
  } catch (err) {
    logAiWarn('Không đọc được generated image assets từ AI', err)
    return undefined
  }
}

function parseWarnings(raw?: string): string[] | undefined {
  if (!raw) return undefined
  try {
    const parsed = JSON.parse(raw)
    const data = parsed?.data ?? parsed?.result?.data ?? parsed?.result ?? parsed
    return Array.isArray(data?.warnings) ? data.warnings.filter((x: unknown): x is string => typeof x === 'string') : undefined
  } catch (err) {
    logAiWarn('Không đọc được warnings từ AI', err)
    return undefined
  }
}

function isHttpUrl(value: unknown): value is string {
  if (typeof value !== 'string' || !value.trim()) return false
  try {
    const url = new URL(value)
    return url.protocol === 'http:' || url.protocol === 'https:'
  } catch {
    return false
  }
}

function pushImagePreview(images: AiGeneratedImagePreview[], url: unknown, alt?: unknown, label?: unknown, kind?: unknown) {
  if (!isHttpUrl(url)) return
  if (images.some((image) => image.url === url)) return
  images.push({
    url,
    alt: typeof alt === 'string' ? alt : undefined,
    label: typeof label === 'string' ? label : undefined,
    kind: typeof kind === 'string' ? kind : undefined,
  })
}

function parseGeneratedImagesPayload(raw?: string): AiGeneratedImagePreview[] | undefined {
  if (!raw) return undefined

  try {
    const parsed = JSON.parse(raw)
    const data = parsed?.data ?? parsed?.result?.data ?? parsed?.result ?? parsed
    if (data?.kind !== 'blog_generated_images') return undefined

    const images: AiGeneratedImagePreview[] = []
    pushImagePreview(images, data.featuredPublicUrl ?? data.featuredPreviewUrl, 'Ảnh AI đã tạo', 'Ảnh nổi bật', 'featured')

    if (Array.isArray(data.publicImageUrls)) {
      data.publicImageUrls.forEach((url: unknown, index: number) => {
        pushImagePreview(images, url, `Ảnh AI đã tạo ${index + 1}`, `Ảnh ${index + 1}`)
      })
    }

    const collect = (items: unknown) => {
      if (!Array.isArray(items)) return
      items.forEach((rawItem) => {
        const item = rawItem as Record<string, unknown>
        pushImagePreview(images, item.publicUrl ?? item.previewUrl, item.altText, item.label, item.kind)
      })
    }
    collect(data.inlineImages)
    collect(data.galleryImages)

    return images.length > 0 ? images : undefined
  } catch (err) {
    logAiWarn('Không đọc được ảnh sinh từ AI', err)
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
  const blogClarification = toolCall.toolName === 'generate_blog_draft' ? parseBlogClarificationPayload(result) : undefined
  const generatedImages = toolCall.toolName === 'generate_blog_images' || toolCall.toolName === 'generate_blog_draft'
    ? (parseGeneratedImagesPayload(result) ?? parseBlogImageAssets(result)?.map((image) => ({
        url: image.publicUrl,
        alt: image.altText ?? undefined,
        label: image.label ?? undefined,
        kind: image.kind ?? undefined,
      })))
    : undefined

  return {
    ...toolCall,
    result,
    meta: parseToolResultMeta(result),
    blogDraft,
    blogClarification,
    imagePlan: parseImagePlanPayload(result),
    phases: parsePhaseStatusPayload(result),
    warnings: parseWarnings(result),
    generatedImages,
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

function parseToolResultFromStoredMessage(message: AdminConversationMessage): string | undefined {
  if (message.structuredPayloadJson) return message.structuredPayloadJson
  if (message.role === 'tool_response') return message.content
  return undefined
}

function parseToolCalls(message: AdminConversationMessage): AiMessage['toolCalls'] {
  if (!message.toolCallsJson) return undefined
  try {
    const parsed = JSON.parse(message.toolCallsJson) as { name?: string; callId?: string }
    const toolName = parsed.name || parsed.callId || 'unknown'
    const base = makeToolCall(toolName, message.content || '')
    const result = parseToolResultFromStoredMessage(message)
    return [{
      ...base,
      result,
      meta: parseToolResultMeta(result),
      blogDraft: toolName === 'generate_blog_draft' ? parseBlogDraftPayload(result) : undefined,
      blogClarification: toolName === 'generate_blog_draft' ? parseBlogClarificationPayload(result) : undefined,
      imagePlan: parseImagePlanPayload(result),
      phases: parsePhaseStatusPayload(result),
      warnings: parseWarnings(result),
      generatedImages: toolName === 'generate_blog_images' || toolName === 'generate_blog_draft'
        ? (parseGeneratedImagesPayload(result) ?? parseBlogImageAssets(result)?.map((image) => ({
            url: image.publicUrl,
            alt: image.altText ?? undefined,
            label: image.label ?? undefined,
            kind: image.kind ?? undefined,
          })))
        : undefined,
    }]
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

function mergeToolResponses(messages: AdminConversationMessage[]): AdminConversationMessage[] {
  const pendingResults = new Map<string, AdminConversationMessage>()
  const merged: AdminConversationMessage[] = []

  for (const message of messages) {
    if (message.role === 'tool_response') {
      if (message.toolCallsJson) {
        try {
          const parsed = JSON.parse(message.toolCallsJson) as { callId?: string; name?: string }
          if (parsed.callId) pendingResults.set(parsed.callId, message)
        } catch (err) {
          logAiWarn('Không đọc được tool_response để ghép lịch sử', err)
        }
      }
      continue
    }

    if (message.role === 'tool_call' && message.toolCallsJson) {
      try {
        const parsed = JSON.parse(message.toolCallsJson) as { callId?: string; name?: string }
        const toolResponse = parsed.callId ? pendingResults.get(parsed.callId) : undefined
        if (toolResponse) {
          merged.push({
            ...message,
            structuredPayloadJson: toolResponse.structuredPayloadJson ?? toolResponse.content,
          })
          pendingResults.delete(parsed.callId!)
          continue
        }
      } catch (err) {
        logAiWarn('Không ghép được tool_call với tool_response', err)
      }
    }

    merged.push(message)
  }

  return merged
}

function mapConversationDetail(detail: AdminConversationDetail): SavedConversation {
  const mergedMessages = mergeToolResponses(detail.messages)
  return {
    id: detail.id,
    conversationId: detail.id,
    title: detail.title || 'Cuộc trò chuyện mới',
    messages: mergedMessages.map(mapServerMessage).filter((m): m is AiMessage => Boolean(m)),
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
  /** Last user message text, stashed so retryLast() can re-send it. */
  lastUserMessage: string | null
  conversationId: string | null
  pendingActions: AiPendingAction[]
  suggestions: AiSuggestion[]
  chatMode: AdminChatMode
  hermesStatus: HermesStatus | null

  // Chat history
  conversations: SavedConversation[]
  activeConversationId: string | null
  suppressNextLoadConversationId: string | null

  toggle: () => void
  open: () => void
  close: () => void
  sendMessage: (req: AiChatRequest) => Promise<void>
  confirmAction: (actionId: string, approved: boolean) => Promise<boolean>
  continueAfterConfirm: (conversationId: string) => Promise<void>
  /** Re-send the last user message after a failed turn. */
  retryLast: () => Promise<void>
  fetchSuggestions: () => Promise<void>
  fetchHermesStatus: () => Promise<void>
  setChatMode: (mode: AdminChatMode) => void
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
  lastUserMessage: null,
  conversationId: null,
  pendingActions: [],
  suggestions: [],
  chatMode: 'generic',
  hermesStatus: null,
  conversations: loadConversations(),
  activeConversationId: null,
  suppressNextLoadConversationId: null,

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
      lastUserMessage: req.message,
      messages: [...s.messages, userMsg, assistantMsg],
    }))

    let sawError = false
    try {
      const endpoint = state.chatMode === 'hermes' ? '/api/admin/hermes/chat' : '/api/admin/ai/chat'
      const response = await fetch(`${API}${endpoint}`, {
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
            } else if (chunk.type === 'tool_error') {
              // Terminal tool failure after retry budget exhausted. Mark the matching
              // tool call and the assistant turn as errored so the UI can offer retry.
              sawError = true
              const toolName = chunk.toolName || chunk.toolCallId || 'unknown'
              const errMsg = chunk.content || 'Công cụ thất bại.'
              const idx = toolCalls.findLastIndex((tc) => tc.toolName === toolName && !tc.error)
              if (idx >= 0) toolCalls[idx] = { ...toolCalls[idx], error: errMsg }
              set((s) => ({
                lastError: errMsg,
                messages: s.messages.map((m) =>
                  m.id === assistantMsg.id
                    ? { ...m, status: 'error' as const, error: errMsg, toolCalls: [...toolCalls] }
                    : m,
                ),
              }))
            } else if (chunk.type === 'confirmation') {
              const pending: AiPendingAction = {
                actionId: chunk.toolCallId || genId(),
                toolName: chunk.toolName || 'unknown',
                description: chunk.content,
                riskLevel: chunk.riskLevel || 'Medium',
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
              sawError = true
              const errMsg = chunk.content || 'AI stream lỗi.'
              set((s) => ({
                lastError: errMsg,
                messages: s.messages.map((m) =>
                  m.id === assistantMsg.id
                    ? { ...m, status: 'error' as const, error: errMsg }
                    : m,
                ),
              }))
            }
          } catch (err) {
            logAiWarn('Bỏ qua SSE chunk lỗi', { length: data.length, err })
          }
        }
      }

      // Mark final. Only clear lastError if the turn actually succeeded.
      set((s) => ({
        isLoading: false,
        lastError: sawError ? s.lastError : null,
        messages: s.messages.map((m) =>
          m.id === assistantMsg.id
            ? { ...m, content: fullText || m.content, toolCalls: toolCalls }
            : m,
        ),
      }))
      get().saveCurrentConversation()
      await get().fetchConversations()
    } catch (err) {
      sawError = true
      const errorMsg = err instanceof Error ? err.message : 'Unknown error'
      set((s) => ({
        isLoading: false,
        lastError: errorMsg,
        messages: s.messages.map((m) =>
          m.id === assistantMsg.id
            ? { ...m, status: 'error' as const, error: errorMsg }
            : m,
        ),
      }))
    }
  },

  retryLast: async () => {
    const state = get()
    if (state.isLoading) return
    if (!state.lastUserMessage) return

    // Drop the trailing failed assistant bubble AND the user message that triggered it,
    // so sendMessage() re-adding a fresh pair doesn't leave a duplicate user bubble.
    set((s) => {
      const last = s.messages[s.messages.length - 1]
      const dropFailed = last && last.role === 'assistant' && last.status === 'error'
      if (!dropFailed) return { lastError: null }
      const beforeAssistant = s.messages.slice(0, -1)
      const prev = beforeAssistant[beforeAssistant.length - 1]
      const dropUserToo = prev && prev.role === 'user' && prev.content === state.lastUserMessage
      return {
        messages: dropUserToo ? beforeAssistant.slice(0, -1) : beforeAssistant,
        lastError: null,
      }
    })

    await get().sendMessage({
      message: state.lastUserMessage,
      conversationId: state.conversationId ?? undefined,
    })
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

    let sawError = false
    try {
      // Respect the current chat mode (was previously hardcoded to the generic endpoint,
      // which broke continuation after confirming a tool in Hermes mode).
      const endpoint = state.chatMode === 'hermes' ? '/api/admin/hermes/chat' : '/api/admin/ai/chat'
      const response = await fetch(`${API}${endpoint}`, {
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
            } else if (chunk.type === 'tool_error') {
              sawError = true
              const toolName = chunk.toolName || chunk.toolCallId || 'unknown'
              const errMsg = chunk.content || 'Công cụ thất bại.'
              const idx = toolCalls.findLastIndex((tc) => tc.toolName === toolName && !tc.error)
              if (idx >= 0) toolCalls[idx] = { ...toolCalls[idx], error: errMsg }
              set((s) => ({
                lastError: errMsg,
                messages: s.messages.map((m) =>
                  m.id === assistantMsg.id
                    ? { ...m, status: 'error' as const, error: errMsg, toolCalls: [...toolCalls] }
                    : m,
                ),
              }))
            } else if (chunk.type === 'confirmation') {
              const pending: AiPendingAction = {
                actionId: chunk.toolCallId || genId(),
                toolName: chunk.toolName || 'unknown',
                description: chunk.content,
                riskLevel: chunk.riskLevel || 'Medium',
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
              sawError = true
              const errMsg = chunk.content || 'AI stream lỗi.'
              set((s) => ({
                lastError: errMsg,
                messages: s.messages.map((m) =>
                  m.id === assistantMsg.id
                    ? { ...m, status: 'error' as const, error: errMsg }
                    : m,
                ),
              }))
            }
          } catch (err) {
            logAiWarn('Bỏ qua SSE chunk lỗi khi tiếp tục', { length: data.length, err })
          }
        }
      }

      set((s) => ({
        isLoading: false,
        lastError: sawError ? s.lastError : null,
        messages: s.messages.map((m) =>
          m.id === assistantMsg.id
            ? { ...m, content: fullText || m.content, toolCalls }
            : m,
        ),
      }))
      get().saveCurrentConversation()
      await get().fetchConversations()
    } catch (err) {
      sawError = true
      const errorMsg = err instanceof Error ? err.message : 'Unknown error'
      logAiWarn('Tiếp tục AI sau xác nhận thất bại', err)
      set((s) => ({
        isLoading: false,
        lastError: errorMsg,
        messages: s.messages.map((m) =>
          m.id === assistantMsg.id
            ? { ...m, status: 'error' as const, error: errorMsg }
            : m,
        ),
      }))
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

  fetchHermesStatus: async () => {
    try {
      const status = await request<HermesStatus>('/api/admin/hermes/status')
      set({ hermesStatus: status })
    } catch (err) {
      logAiWarn('Không tải được trạng thái Hermes', err)
      set({ hermesStatus: { status: 'offline', runnerName: 'aodai-admin-hermes', lastHeartbeatAt: null, model: null, gatewayStatus: null, activeJobs: 0, lastError: 'Không tải được trạng thái Hermes.', apiServerConfigured: false } })
    }
  },

  setChatMode: (mode: AdminChatMode) => {
    set({ chatMode: mode })
    if (mode === 'hermes') void get().fetchHermesStatus()
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
        toolCalls: m.toolCalls?.map((tc) => ({ ...tc })),
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
    const suppressId = get().suppressNextLoadConversationId
    if (suppressId === id) {
      set({ suppressNextLoadConversationId: null })
      return
    }

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
        suppressNextLoadConversationId: null,
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
        suppressNextLoadConversationId: null,
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
    const { activeConversationId, conversationId } = get()
    set({
      messages: [],
      conversationId: null,
      pendingActions: [],
      activeConversationId: null,
      suppressNextLoadConversationId: activeConversationId ?? conversationId,
      lastError: null,
      lastUserMessage: null,
    })
  },
}))
