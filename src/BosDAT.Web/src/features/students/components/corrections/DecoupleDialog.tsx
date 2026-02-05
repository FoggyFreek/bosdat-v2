import { memo } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import type { LedgerApplication } from '@/features/students/types'

interface DecoupleDialogProps {
  readonly open: boolean
  readonly application: LedgerApplication | null
  readonly reason: string
  readonly isPending: boolean
  readonly onOpenChange: (open: boolean) => void
  readonly onReasonChange: (reason: string) => void
  readonly onConfirm: () => void
}

export const DecoupleDialog = memo(function DecoupleDialog({
  open,
  application,
  reason,
  isPending,
  onOpenChange,
  onReasonChange,
  onConfirm,
}: DecoupleDialogProps) {
  const isValid = reason.trim().length > 0

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Decouple Correction</DialogTitle>
          <DialogDescription>
            This will remove the link between this correction and invoice{' '}
            <strong>{application?.invoiceNumber}</strong> (€{application?.appliedAmount.toFixed(2)}).
            The credit will become available to apply to another invoice.
          </DialogDescription>
        </DialogHeader>
        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="decoupleReason">Reason</Label>
            <Input
              id="decoupleReason"
              value={reason}
              onChange={(e) => onReasonChange(e.target.value)}
              placeholder="e.g., Applied to wrong invoice"
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button variant="destructive" onClick={onConfirm} disabled={!isValid || isPending}>
            {isPending ? 'Decoupling...' : 'Decouple'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
})
