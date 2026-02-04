import { memo } from 'react'
import { Undo2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { formatDate } from '@/lib/datetime-helpers'
import type { StudentLedgerEntry } from '@/features/students/types'
import { EntryTypeBadge, StatusBadge, EntryAmount } from './EntryBadges'

interface LedgerEntryRowProps {
  readonly entry: Readonly<StudentLedgerEntry>
  readonly onReverse: (entryId: string) => void
}

export const LedgerEntryRow = memo(function LedgerEntryRow({
  entry,
  onReverse,
}: LedgerEntryRowProps) {
  const showRemainingAmount = entry.remainingAmount !== entry.amount
  const canReverse = entry.status === 'Open'

  return (
    <div className="flex items-center justify-between py-3">
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
  )
})
