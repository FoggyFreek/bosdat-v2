import { memo, useState } from 'react'
import { ChevronDown, ChevronRight, Undo2, Unlink } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { formatDate } from '@/lib/datetime-helpers'
import type { StudentLedgerEntry, LedgerApplication } from '@/features/students/types'
import { EntryTypeBadge, StatusBadge, EntryAmount } from './EntryBadges'

interface LedgerEntryRowProps {
  readonly entry: Readonly<StudentLedgerEntry>
  readonly onReverse: (entryId: string) => void
  readonly onDecouple: (application: LedgerApplication) => void
}

export const LedgerEntryRow = memo(function LedgerEntryRow({
  entry,
  onReverse,
  onDecouple,
}: LedgerEntryRowProps) {
  const [expanded, setExpanded] = useState(false)
  const showRemainingAmount = entry.remainingAmount !== entry.amount
  const canReverse = entry.status === 'Open'
  const hasApplications = entry.applications.length > 0

  return (
    <div className="py-3">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          {hasApplications && (
            <button
              type="button"
              className="text-muted-foreground hover:text-foreground"
              onClick={() => setExpanded(!expanded)}
              aria-expanded={expanded}
              aria-label={expanded ? 'Collapse applications' : 'Expand applications'}
            >
              {expanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
            </button>
          )}
          <div>
            <div className="flex items-center gap-2">
              <span className="font-mono text-sm text-muted-foreground">
                {entry.correctionRefName}
              </span>
              <EntryTypeBadge type={entry.entryType} />
              <StatusBadge status={entry.status} />
            </div>
            <p className="font-medium">{entry.description}</p>
            <p className="text-sm text-muted-foreground">
              {formatDate(entry.createdAt)} by {entry.createdByName}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-4">
          <div className="text-right">
            <EntryAmount amount={entry.amount} entryType={entry.entryType} />
            {showRemainingAmount && (
              <p className="text-xs text-muted-foreground">
                Remaining: €{entry.remainingAmount.toFixed(2)}
              </p>
            )}
          </div>
          {canReverse && (
            <Button
              variant="ghost"
              size="sm"
              className="h-8 text-orange-600 hover:text-orange-700 hover:bg-orange-50"
              onClick={() => onReverse(entry.id)}
            >
              <Undo2 className="h-4 w-4 mr-1" />
              Reverse
            </Button>
          )}
        </div>
      </div>

      {expanded && hasApplications && (
        <div className="ml-6 mt-2 space-y-1 border-l-2 border-muted pl-3">
          <p className="text-xs font-medium text-muted-foreground uppercase">Applied to invoices</p>
          {entry.applications.map((app) => (
            <div key={app.id} className="flex items-center justify-between text-sm bg-muted/30 rounded px-3 py-1.5">
              <div className="flex items-center gap-3">
                <span className="font-mono">{app.invoiceNumber}</span>
                <span className="text-muted-foreground">€{app.appliedAmount.toFixed(2)}</span>
                <span className="text-xs text-muted-foreground">
                  {formatDate(app.appliedAt)} by {app.appliedByName}
                </span>
              </div>
              <Button
                variant="ghost"
                size="sm"
                className="h-7 text-red-600 hover:text-red-700 hover:bg-red-50"
                onClick={() => onDecouple(app)}
              >
                <Unlink className="h-3.5 w-3.5 mr-1" />
                Decouple
              </Button>
            </div>
          ))}
        </div>
      )}
    </div>
  )
})
