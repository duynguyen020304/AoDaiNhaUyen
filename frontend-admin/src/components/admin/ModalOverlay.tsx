import { X } from 'lucide-react'
import { cn } from '@/lib/utils'

interface ModalOverlayProps {
  open: boolean
  onClose: () => void
  children: React.ReactNode
  className?: string
}

export function ModalOverlay({ open, onClose, children, className }: ModalOverlayProps) {
  if (!open) return null

  return (
    <div
      role="dialog"
      aria-modal="true"
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) onClose()
      }}
    >
      <div className={cn('relative w-full max-w-lg rounded-xl bg-white shadow-lg', className)}>
        <button
          onClick={onClose}
          className="absolute right-3 top-3 text-muted-foreground hover:text-foreground transition-colors"
          aria-label="Đóng"
        >
          <X className="size-5" />
        </button>
        {children}
      </div>
    </div>
  )
}
