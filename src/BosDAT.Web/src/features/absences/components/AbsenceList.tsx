import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { CalendarX, Pencil, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { absencesApi } from '@/features/absences/api'
import type { Absence } from '@/features/absences/types'
import { absenceReasonTranslations } from '@/features/absences/types'

const reasonBadgeColors: Record<string, string> = {
  Holiday: 'bg-blue-100 text-blue-800',
  Sick: 'bg-red-100 text-red-800',
  Other: 'bg-gray-100 text-gray-800',
}

interface AbsenceListProps {
  absences: Absence[]
  studentId?: string
  teacherId?: string
  onEdit: (absence: Absence) => void
}

export function AbsenceList({ absences, studentId, teacherId, onEdit }: Readonly<AbsenceListProps>) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [deleteId, setDeleteId] = useState<string | null>(null)

  const deleteMutation = useMutation({
    mutationFn: (id: string) => absencesApi.delete(id),
    onSuccess: () => {
      if (studentId) {
        queryClient.invalidateQueries({ queryKey: ['absences', 'student', studentId] })
      }
      if (teacherId) {
        queryClient.invalidateQueries({ queryKey: ['absences', 'teacher', teacherId] })
      }
      queryClient.invalidateQueries({ queryKey: ['calendar'] })
      setDeleteId(null)
    },
  })

  if (absences.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-center">
        <CalendarX className="h-12 w-12 text-muted-foreground/50 mb-4" />
        <p className="text-lg font-medium text-muted-foreground">
          {t('students.absence.noAbsences')}
        </p>
      </div>
    )
  }

  return (
    <>
      <div className="divide-y">
        {absences.map((absence) => (
          <div key={absence.id} className="flex items-center justify-between py-3">
            <div className="flex items-center gap-4">
              <div>
                <div className="flex items-center gap-2">
                  <span className="font-medium">
                    {absence.startDate} — {absence.endDate}
                  </span>
                  <Badge variant="secondary" className={reasonBadgeColors[absence.reason] ?? ''}>
                    {t(absenceReasonTranslations[absence.reason])}
                  </Badge>
                  {absence.invoiceLesson && (
                    <Badge variant="outline">{t('students.absence.invoiceLesson')}</Badge>
                  )}
                </div>
                {absence.notes && (
                  <p className="text-sm text-muted-foreground mt-1">{absence.notes}</p>
                )}
              </div>
            </div>
            <div className="flex items-center gap-1">
              <Button variant="ghost" size="icon" onClick={() => onEdit(absence)}>
                <Pencil className="h-4 w-4" />
              </Button>
              <Button
                variant="ghost"
                size="icon"
                onClick={() => setDeleteId(absence.id)}
              >
                <Trash2 className="h-4 w-4 text-destructive" />
              </Button>
            </div>
          </div>
        ))}
      </div>

      <AlertDialog open={!!deleteId} onOpenChange={() => setDeleteId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('students.absence.deleteAbsence')}</AlertDialogTitle>
            <AlertDialogDescription>
              {t('students.absence.deleteConfirm')}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('common.actions.cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => deleteId && deleteMutation.mutate(deleteId)}
              disabled={deleteMutation.isPending}
            >
              {t('common.actions.delete')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
