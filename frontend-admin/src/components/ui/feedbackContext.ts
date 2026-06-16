import { createContext, useContext } from 'react'

type ToastKind = 'success' | 'error' | 'info'
export type ConfirmOptions = { title: string; message: string; confirmText?: string; cancelText?: string; destructive?: boolean }

export interface FeedbackContextValue {
  toast: (message: string, kind?: ToastKind) => void
  confirm: (options: ConfirmOptions) => Promise<boolean>
}

export const FeedbackContext = createContext<FeedbackContextValue | null>(null)

export function useFeedback() {
  const context = useContext(FeedbackContext)
  if (!context) throw new Error('useFeedback must be used within FeedbackProvider')
  return context
}
