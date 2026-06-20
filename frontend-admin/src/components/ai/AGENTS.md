<!-- Parent: ../../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin/src/components/ai

## Purpose
Admin AI chat UI components. Together they form the floating sidebar chat panel that supports both generic admin chat and Hermes agent mode, with SSE streaming, tool call display, action confirmation cards, and conversation history.

## Key Files
| File | Description |
|------|-------------|
| `AiChatSidebar.tsx` | Root sidebar panel: fixed `aside` overlay, header, mode selector, message list, text input; reads/writes `adminAiStore` |
| `FullChatArea.tsx` | Full-page chat layout used by `AiChatPage` (as opposed to the sidebar overlay) |
| `MessageBubble.tsx` | Renders a single `AiMessage`: user/assistant bubbles, tool call disclosure, pending action display |
| `ChatInput.tsx` | Text input + send button; handles Enter-to-send |
| `ChatModeSelector.tsx` | Toggle between `'generic'` and `'hermes'` chat modes; calls `adminAiStore.setChatMode()` |
| `ChatHistorySidebar.tsx` | Conversation list panel; load, delete, new conversation actions |
| `ConfirmCard.tsx` | Inline approve/reject card rendered inside a `MessageBubble` for pending AI tool actions |
| `EmptyChat.tsx` | Placeholder shown when no messages exist |

## For AI Agents
### Working In This Directory
- All components read from `useAdminAiStore` — no local async state, no direct API calls.
- SSE streaming state lives entirely in `adminAiStore.sendMessage()`; components only display derived state.
- `MessageBubble` must handle all `AiMessage` shapes: plain text, tool calls (with/without results), and pending confirmation actions.
- `ConfirmCard` calls `adminAiStore.confirmAction(actionId, approved)` then `continueAfterConfirm(conversationId)`.
- Hermes mode adds a status indicator in the header; `setChatMode('hermes')` triggers `fetchHermesStatus()` automatically.

### Common Patterns
- Scroll to bottom on new messages: `useEffect` on `messages` with a `scrollRef.current.scrollTop = scrollRef.current.scrollHeight`.
- Sidebar is `fixed right-0 top-0 h-dvh w-96 z-50` — do not change z-index without checking `ModalOverlay` (`z-50`) and `FeedbackProvider` toast (`z-[70]`/`z-[80]`).
- All UI strings are in Vietnamese.

## Dependencies
### Internal
- `@/stores/adminAiStore` — all chat state and actions
- `@/components/ui/button` — Button primitive
- `@/types/ai` — `AiMessage`, `AiToolCall`, `AiPendingAction`, `AdminChatMode`, `HermesStatus`

### External
- lucide-react (Bot, X, Send, Loader2 icons)
- react-markdown + remark-gfm (markdown rendering in message bubbles)

<!-- MANUAL: -->
