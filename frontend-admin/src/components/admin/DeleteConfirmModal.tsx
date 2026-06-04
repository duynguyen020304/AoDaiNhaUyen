import { useState } from 'react'
import { Loader2, AlertTriangle } from 'lucide-react'
import { ModalOverlay } from './ModalOverlay'
import { Button } from '@/components/ui/button'

interface Props {
  open: boolean
  onClose: () => void
  onConfirm: () => Promise<void>
  title: string
  message: string
}

export function DeleteConfirmModal({ open, onClose, onConfirm, title, message }: Props) {
  const [loading, setLoading] = useState(false)

  async function handleConfirm() {
    setLoading(true)
    try {
      await onConfirm()
      onClose()
    } catch {
      // error handled by caller
    } finally {
      setLoading(false)
    }
  }

  return (
    <ModalOverlay open={open} onClose={onClose}>
      <div className="p-6 text-center">
        <div className="mx-auto mb-4 flex size-12 items-center justify-center rounded-full bg-destructive/10">
          <AlertTriangle className="size-6 text-destructive" />
        </div>
        <h2 className="text-lg font-semibold mb-2">{title}</h2>
        <p className="text-sm text-muted-foreground mb-6">{message}</p>
        <div className="flex justify-center gap-3">
          <Button variant="outline" onClick={onClose} disabled={loading}>
            Hủy
          </Button>
          <Button variant="destructive" onClick={handleConfirm} disabled={loading}>
            {loading && <Loader2 className="size-4 animate-spin" />}
            Xóa
          </Button>
        </div>
      </div>
    </ModalOverlay>
  )
}
