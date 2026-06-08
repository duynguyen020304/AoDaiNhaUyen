/** SSE chunk types from POST /api/admin/ai/chat */
export interface AiLlmChunk {
  type: 'text' | 'tool_call' | 'tool_result' | 'confirmation' | 'error' | 'done'
  content: string
  toolName?: string
  toolCallId?: string
}

export interface AiMessage {
  id: string
  role: 'user' | 'assistant' | 'system'
  content: string
  /** Tool calls made within this message */
  toolCalls?: AiToolCall[]
  /** Pending confirmation action */
  pendingAction?: AiPendingAction
  createdAt: string
}

export interface AiToolCall {
  toolName: string
  input: string
  result?: string
  riskLevel?: string
}

export interface AiPendingAction {
  actionId: string
  toolName: string
  description: string
  riskLevel: string
  requestedAt: string
  status?: 'pending' | 'confirmed' | 'rejected'
}

export interface AiSuggestion {
  id: string
  title: string
  description: string
  route?: string
}

export interface AiChatRequest {
  message: string
  conversationId?: string
}

export interface AiConfirmRequest {
  actionId: string
  approved: boolean
}
export interface SavedConversation {
  id: string
  conversationId?: string | null
  title: string
  messages: AiMessage[]
  createdAt: string
  updatedAt: string
}
