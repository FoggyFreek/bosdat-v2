import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { absencesApi } from '@/features/absences/api'
import type { Absence, AbsenceReason, CreateAbsence, UpdateAbsence } from '@/features/absences/types'
import { absenceReasonTranslations } from '@/features/absences/types'

const ABSENCE_REASONS: AbsenceReason[] = ['Holiday', 'Sick', 'Other']

interface AbsenceDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  studentId?: string
  teacherId?: string
  absence?: Absence | null
}

interface AbsenceDialogContentProps extends AbsenceDialogProps {
  isEdit: boolean
}

function AbsenceDialogContent({
  onOpenChange,
  studentId,
  teacherId,
  absence,
  isEdit,
}: Readonly<AbsenceDialogContentProps>) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const [startDate, setStartDate] = useState(absence?.startDate ?? '')
  const [endDate, setEndDate] = useState(absence?.endDate ?? '')
  const [reason, setReason] = useState<AbsenceReason>(absence?.reason ?? 'Sick')
  const [notes, setNotes] = useState(absence?.notes ?? '')
  const [invoiceLesson, setInvoiceLesson] = useState(absence?.invoiceLesson ?? false)

  const invalidateKeys = () => {
    if (studentId) {
      queryClient.invalidateQueries({ queryKey: ['absences', 'student', studentId] })
    }
    if (teacherId) {
      queryClient.invalidateQueries({ queryKey: ['absences', 'teacher', teacherId] })
    }
    queryClient.invalidateQueries({ queryKey: ['calendar'] })
  }

  const createMutation = useMutation({
    mutationFn: (data: CreateAbsence) => absencesApi.create(data),
    onSuccess: () => {
      invalidateKeys()
      onOpenChange(false)
    },
  })

  const updateMutation = useMutation({
    mutationFn: (data: UpdateAbsence) => absencesApi.update(absence!.id, data),
    onSuccess: () => {
      invalidateKeys()
      onOpenChange(false)
    },
  })

  const isPending = createMutation.isPending || updateMutation.isPending
  const canSubmit = startDate && endDate && startDate <= endDate && reason

  const handleSubmit = () => {
    if (!canSubmit) return

    if (isEdit) {
      updateMutation.mutate({
        startDate,
        endDate,
        reason,
        notes: notes || undefined,
        invoiceLesson,
      })
    } else {
      createMutation.mutate({
        studentId,
        teacherId,
        startDate,
        endDate,
        reason,
        notes: notes || undefined,
        invoiceLesson,
      })
    }
  }

  return (
    <DialogContent>
      <DialogHeader>
        <DialogTitle>
          {isEdit ? t('students.absence.editAbsence') : t('students.absence.addAbsence')}
        </DialogTitle>
        <DialogDescription>
          {t('students.absence.invoiceLessonHelp')}
        </DialogDescription>
      </DialogHeader>

      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="absence-start">{t('students.absence.startDate')}</Label>
            <Input
              id="absence-start"
              type="date"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="absence-end">{t('students.absence.endDate')}</Label>
            <Input
              id="absence-end"
              type="date"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
            />
          </div>
        </div>

        <div className="space-y-2">
          <Label>{t('students.absence.reason')}</Label>
          <Select value={reason} onValueChange={(val) => setReason(val as AbsenceReason)}>
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {ABSENCE_REASONS.map((r) => (
                <SelectItem key={r} value={r}>
                  {t(absenceReasonTranslations[r])}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-2">
          <Label htmlFor="absence-notes">{t('students.absence.notes')}</Label>
          <Textarea
            id="absence-notes"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={2}
          />
        </div>

        <div className="flex items-center space-x-2">
          <Checkbox
            id="invoice-lesson"
            checked={invoiceLesson}
            onCheckedChange={(checked) => setInvoiceLesson(checked === true)}
          />
          <Label htmlFor="invoice-lesson" className="text-sm font-normal">
            {t('students.absence.invoiceLesson')}
          </Label>
        </div>
      </div>

      <DialogFooter>
        <Button variant="outline" onClick={() => onOpenChange(false)}>
          {t('common.actions.cancel')}
        </Button>
        <Button onClick={handleSubmit} disabled={!canSubmit || isPending}>
          {isPending ? t('common.actions.save') + '...' : t('common.actions.save')}
        </Button>
      </DialogFooter>
    </DialogContent>
  )
}

export function AbsenceDialog({
  open,
  onOpenChange,
  studentId,
  teacherId,
  absence,
}: Readonly<AbsenceDialogProps>) {
  const isEdit = !!absence

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <AbsenceDialogContent
        key={`${absence?.id ?? 'new'}-${String(open)}`}
        open={open}
        onOpenChange={onOpenChange}
        studentId={studentId}
        teacherId={teacherId}
        absence={absence}
        isEdit={isEdit}
      />
    </Dialog>
  )
}
