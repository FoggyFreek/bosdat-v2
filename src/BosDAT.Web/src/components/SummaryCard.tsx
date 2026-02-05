import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

export interface SummaryItem {
  label: string
  value: ReactNode
}

interface SummaryCardProps {
  title: string
  items?: SummaryItem[]
  children?: ReactNode
  className?: string
}

export function SummaryCard({ title, items, children, className }: SummaryCardProps) {
  return (
    <div className={cn('rounded-lg border bg-muted/50 p-4', className)}>
      <h3 className="font-medium mb-3 text-sm">{title}</h3>
      {items && items.length > 0 && (
        <div className="grid gap-2 text-sm">
          {items.map((item) => (
            <div key={item.label} className="flex justify-between">
              <span className="text-muted-foreground">{item.label}</span>
              <span className="font-medium">{item.value}</span>
            </div>
          ))}
        </div>
      )}
      {children}
    </div>
  )
}
