import type { ReactNode } from 'react'
import { Card, CardContent } from '@/components/ui/card'

interface FacebookEmptyStateProps {
  icon?: ReactNode
  title: string
  description?: string
  action?: ReactNode
}

export function FacebookEmptyState({ icon, title, description, action }: FacebookEmptyStateProps) {
  return (
    <Card className="border-dashed bg-white/70">
      <CardContent className="flex min-h-48 flex-col items-center justify-center px-6 py-10 text-center">
        {icon && <div className="mb-3 text-primary/70">{icon}</div>}
        <h3 className="text-base font-semibold text-ink">{title}</h3>
        {description && <p className="mt-1 max-w-md text-sm text-muted-foreground">{description}</p>}
        {action && <div className="mt-4">{action}</div>}
      </CardContent>
    </Card>
  )
}
