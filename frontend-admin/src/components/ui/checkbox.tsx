import * as React from 'react'
import { cn } from '@/lib/utils'

export function Checkbox({ className, ...props }: React.InputHTMLAttributes<HTMLInputElement>) {
  return (
    <input
      type="checkbox"
      className={cn(
        'peer size-4 shrink-0 rounded border border-input accent-primary focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:opacity-50',
        className
      )}
      {...props}
    />
  )
}
