import * as React from 'react'
import { cn } from '@/lib/utils'
import { X } from 'lucide-react'

export function Sheet({ open, onOpenChange, children }: { open?: boolean; onOpenChange?: (v: boolean) => void; children: React.ReactNode }) {
  if (!open) return null
  return (
    <div className="fixed inset-0 z-50">
      <div className="fixed inset-0 bg-black/40" onClick={() => onOpenChange?.(false)} />
      <div className="fixed inset-y-0 left-0 w-64 bg-card shadow-lg animate-in slide-in-from-left">{children}</div>
    </div>
  )
}
export function SheetTrigger({ children, onClick }: { children: React.ReactNode; onClick?: () => void }) {
  return <span onClick={onClick}>{children}</span>
}
export function SheetHeader({ className, children, onOpenChange }: { className?: string; children: React.ReactNode; onOpenChange?: (v: boolean) => void }) {
  return (
    <div className={cn('flex items-center justify-between p-4 border-b', className)}>
      {children}
      <button onClick={() => onOpenChange?.(false)} className="rounded-md p-1 hover:bg-muted">
        <X className="size-4" />
      </button>
    </div>
  )
}
