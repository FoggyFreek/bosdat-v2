import { AlertCircle } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import type { ConflictingCourse } from '../types'

interface ConflictDialogProps {
  readonly open: boolean
  readonly conflicts: ConflictingCourse[]
  readonly onClose: () => void
}

export function ConflictDialog({ open, conflicts, onClose }: ConflictDialogProps) {
  const safeConflicts = conflicts || []

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Schedule Conflict Detected</DialogTitle>
          <DialogDescription>
            This course conflicts with existing enrollment(s). Please choose a different course or
            time slot.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-2 max-h-96 overflow-y-auto">
          {safeConflicts.length === 0 ? (
            <p className="text-sm text-muted-foreground">No conflicts to display.</p>
          ) : (
            safeConflicts.map((conflict) => (
              <Alert key={conflict.courseId} variant="destructive">
                <AlertCircle className="h-4 w-4" />
                <AlertTitle>{conflict.courseName}</AlertTitle>
                <AlertDescription>
                  <div className="space-y-1 text-sm">
                    <div>
                      {conflict.dayOfWeek} {conflict.timeSlot}
                    </div>
                    <div>
                      {conflict.frequency}
                      {conflict.weekParity && ` - ${conflict.weekParity} Weeks`}
                    </div>
                  </div>
                </AlertDescription>
              </Alert>
            ))
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            Choose Different Course
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
