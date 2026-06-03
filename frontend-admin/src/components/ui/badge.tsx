import * as React from 'react'
import { cn } from '@/lib/utils'

export function Badge({ className, variant = 'default', ...props }: React.HTMLAttributes<HTMLDivElement> & { variant?: 'default' | 'outline' | 'success' | 'warning' }) {
  const variantClasses: Record<string, string> = {
    default: 'bg-primary text-primary-foreground',
    outline: 'border border-primary/30 text-primary bg-primary/5',
    success: 'bg-green-50 text-green-700 border border-green-200',
    warning: 'bg-yellow-50 text-yellow-700 border border-yellow-200',
  }
  return (
    <div
      className={cn('inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium', variantClasses[variant], className)}
      {...props}
    />
  )
}
