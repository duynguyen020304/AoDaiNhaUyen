import { useCallback, useMemo, useState } from 'react'
import { Button } from '@/components/ui/button'
import { FeedbackContext } from '@/components/ui/feedbackContext'
import type { ConfirmOptions } from '@/components/ui/feedbackContext'

type ToastKind = 'success' | 'error' | 'info'
type Toast = { id: number; kind: ToastKind; message: string }
type ConfirmState = ConfirmOptions & { resolve: (value: boolean) => void }

export function FeedbackProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])
  const [confirmState, setConfirmState] = useState<ConfirmState | null>(null)

  const toast = useCallback((message: string, kind: ToastKind = 'info') => {
    const id = Date.now() + Math.random()
    setToasts((items) => [...items, { id, kind, message }])
    window.setTimeout(() => {
      setToasts((items) => items.filter((item) => item.id !== id))
    }, 3500)
  }, [])

  const confirm = useCallback((options: ConfirmOptions) => {
    return new Promise<boolean>((resolve) => setConfirmState({ ...options, resolve }))
  }, [])

  const value = useMemo(() => ({ toast, confirm }), [toast, confirm])

  function closeConfirm(value: boolean) {
    confirmState?.resolve(value)
    setConfirmState(null)
  }

  return (
    <FeedbackContext.Provider value={value}>
      {children}
      <div className="fixed right-4 top-4 z-[70] space-y-2">
        {toasts.map((item) => (
          <div
            key={item.id}
            className={`min-w-72 rounded-lg border px-4 py-3 text-sm shadow-lg ${
              item.kind === 'success'
                ? 'border-green-200 bg-green-50 text-green-800'
                : item.kind === 'error'
                  ? 'border-red-200 bg-red-50 text-red-800'
                  : 'border-gray-200 bg-white text-gray-800'
            }`}
          >
            {item.message}
          </div>
        ))}
      </div>
      {confirmState && (
        <div className="fixed inset-0 z-[80] flex items-center justify-center bg-black/40 p-4" role="dialog" aria-modal="true">
          <div className="w-full max-w-md rounded-xl bg-white p-5 shadow-xl">
            <h2 className="text-lg font-semibold text-burgundy">{confirmState.title}</h2>
            <p className="mt-2 text-sm text-gray-600">{confirmState.message}</p>
            <div className="mt-5 flex justify-end gap-2">
              <Button type="button" variant="outline" onClick={() => closeConfirm(false)}>
                {confirmState.cancelText ?? 'Hủy'}
              </Button>
              <Button
                type="button"
                variant={confirmState.destructive ? 'destructive' : 'default'}
                onClick={() => closeConfirm(true)}
              >
                {confirmState.confirmText ?? 'Xác nhận'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </FeedbackContext.Provider>
  )
}
