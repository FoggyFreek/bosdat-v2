import { useTranslation } from 'react-i18next'
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
import { dayOfWeekTranslations, type DayOfWeek } from '@/lib/datetime-helpers'
import { courseFrequencyTranslations, weekParityTranslations, type CourseFrequency, type WeekParity } from '@/features/courses/types'

interface ConflictDialogProps {
  readonly open: boolean
  readonly conflicts: ConflictingCourse[]
  readonly onClose: () => void
}

export function ConflictDialog({ open, conflicts, onClose }: ConflictDialogProps) {
  const { t } = useTranslation()
  const safeConflicts = conflicts || []

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('enrollments.conflicts.title')}</DialogTitle>
          <DialogDescription>
            {t('enrollments.conflicts.description')}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-2 max-h-96 overflow-y-auto">
          {safeConflicts.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t('enrollments.conflicts.noConflicts')}</p>
          ) : (
            safeConflicts.map((conflict) => (
              <Alert key={conflict.courseId} variant="destructive">
                <AlertCircle className="h-4 w-4" />
                <AlertTitle>{conflict.courseName}</AlertTitle>
                <AlertDescription>
                  <div className="space-y-1 text-sm">
                    <div>
                      {t(dayOfWeekTranslations[conflict.dayOfWeek as DayOfWeek])} {conflict.timeSlot}
                    </div>
                    <div>
                      {t(courseFrequencyTranslations[conflict.frequency as CourseFrequency])}
                      {conflict.weekParity && ` - ${t(weekParityTranslations[conflict.weekParity as WeekParity])}`}
                    </div>
                  </div>
                </AlertDescription>
              </Alert>
            ))
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            {t('enrollments.conflicts.chooseDifferentCourse')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
