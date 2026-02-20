import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { useToast } from '@/hooks/use-toast'
import { lessonsApi } from '@/features/lessons/api'
import { formatDate, formatTime } from '@/lib/datetime-helpers'
import type { Lesson, LessonStatus } from '@/features/lessons/types'

interface CancelLessonDialogProps {
  readonly lesson: Lesson | null
  readonly courseId: string
  readonly onClose: () => void
}

export function CancelLessonDialog({ lesson, courseId, onClose }: CancelLessonDialogProps) {
  const { toast } = useToast()
  const queryClient = useQueryClient()
  const [reason, setReason] = useState('')
  const [selectedStatus, setSelectedStatus] = useState<'Cancelled' | 'NoShow' | null>(null)

  const mutation = useMutation({
    mutationFn: ({ status, cancellationReason }: { status: LessonStatus; cancellationReason: string }) =>
      lessonsApi.updateStatus(lesson!.id, { status, cancellationReason }),
    onSuccess: (_data, variables) => {
      const statusLabel = variables.status === 'Cancelled' ? 'cancelled' : 'marked as no-show'
      toast({
        title: 'Lesson updated',
        description: `The lesson has been ${statusLabel}.`,
      })
      queryClient.invalidateQueries({ queryKey: ['course', courseId, 'lessons'] })
      queryClient.invalidateQueries({ queryKey: ['lessons', 'student'] })
      handleClose()
    },
    onError: () => {
      toast({
        title: 'Error',
        description: 'Failed to update the lesson status.',
        variant: 'destructive',
      })
    },
  })

  const handleClose = () => {
    setReason('')
    setSelectedStatus(null)
    onClose()
  }

  const handleSubmit = (status: 'Cancelled' | 'NoShow') => {
    if (!reason.trim()) return
    mutation.mutate({ status, cancellationReason: reason.trim() })
  }

  const isValid = reason.trim().length > 0

  return (
    <Dialog open={!!lesson} onOpenChange={(open) => !open && handleClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Cancel Lesson</DialogTitle>
          {lesson && (
            <DialogDescription>
              {formatDate(lesson.scheduledDate)} at {formatTime(lesson.startTime)} –{' '}
              {formatTime(lesson.endTime)}
              {lesson.studentName && ` (${lesson.studentName})`}
            </DialogDescription>
          )}
        </DialogHeader>

        <div className="space-y-4 py-2">
          <div className="space-y-2">
            <Label htmlFor="cancellation-reason">Reason for cancellation *</Label>
            <Textarea
              id="cancellation-reason"
              placeholder="Enter the reason for cancellation..."
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              rows={3}
            />
          </div>

          <div className="space-y-2">
            <Label>Choose action</Label>
            <div className="grid grid-cols-2 gap-3">
              <Button
                variant={selectedStatus === 'Cancelled' ? 'default' : 'outline'}
                className="flex flex-col h-auto py-3"
                disabled={!isValid || mutation.isPending}
                onClick={() => {
                  setSelectedStatus('Cancelled')
                  handleSubmit('Cancelled')
                }}
              >
                <span className="font-medium">Cancel</span>
                <span className="text-xs font-normal opacity-70">Not invoiced</span>
              </Button>
              <Button
                variant={selectedStatus === 'NoShow' ? 'default' : 'outline'}
                className="flex flex-col h-auto py-3"
                disabled={!isValid || mutation.isPending}
                onClick={() => {
                  setSelectedStatus('NoShow')
                  handleSubmit('NoShow')
                }}
              >
                <span className="font-medium">No-Show</span>
                <span className="text-xs font-normal opacity-70">Invoiced</span>
              </Button>
            </div>
          </div>
        </div>

        <DialogFooter>
          <Button variant="ghost" onClick={handleClose} disabled={mutation.isPending}>
            Close
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
