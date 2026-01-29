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

interface ReverseDialogProps {
  readonly open: boolean
  readonly reason: string
  readonly isPending: boolean
  readonly onOpenChange: (open: boolean) => void
  readonly onReasonChange: (reason: string) => void
  readonly onConfirm: () => void
}

export const ReverseDialog = memo(function ReverseDialog({
  open,
  reason,
  isPending,
  onOpenChange,
  onReasonChange,
  onConfirm,
}: ReverseDialogProps) {
  const isValid = reason.trim().length > 0

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Reverse Entry</DialogTitle>
          <DialogDescription>
            This will create an offsetting entry. Please provide a reason for the reversal.
          </DialogDescription>
        </DialogHeader>
        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="reverseReason">Reason</Label>
            <Input
              id="reverseReason"
              value={reason}
              onChange={(e) => onReasonChange(e.target.value)}
              placeholder="e.g., Entered in error"
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={onConfirm} disabled={!isValid || isPending}>
            {isPending ? 'Reversing...' : 'Reverse Entry'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
})
