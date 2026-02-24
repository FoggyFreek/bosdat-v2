import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Plus, CalendarX } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { absencesApi } from '@/features/absences/api'
import { AbsenceDialog } from '@/features/absences/components/AbsenceDialog'
import { AbsenceList } from '@/features/absences/components/AbsenceList'
import type { Absence } from '@/features/absences/types'

interface TeacherAbsenceSectionProps {
  readonly teacherId: string
}

export function TeacherAbsenceSection({ teacherId }: TeacherAbsenceSectionProps) {
  const { t } = useTranslation()
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editAbsence, setEditAbsence] = useState<Absence | null>(null)

  const { data: absences = [], isLoading } = useQuery<Absence[]>({
    queryKey: ['absences', 'teacher', teacherId],
    queryFn: () => absencesApi.getByTeacher(teacherId),
    enabled: !!teacherId,
  })

  const handleEdit = (absence: Absence) => {
    setEditAbsence(absence)
    setDialogOpen(true)
  }

  const handleDialogClose = (open: boolean) => {
    setDialogOpen(open)
    if (!open) setEditAbsence(null)
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <CalendarX className="h-5 w-5" />
            {t('students.sections.absences')}
          </CardTitle>
          <Button size="sm" onClick={() => setDialogOpen(true)}>
            <Plus className="h-4 w-4 mr-2" />
            {t('students.absence.addAbsence')}
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {isLoading && (
          <div className="flex items-center justify-center py-8">
            <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
          </div>
        )}
        {!isLoading && (
          <AbsenceList
            absences={absences}
            teacherId={teacherId}
            onEdit={handleEdit}
          />
        )}
      </CardContent>

      <AbsenceDialog
        open={dialogOpen}
        onOpenChange={handleDialogClose}
        teacherId={teacherId}
        absence={editAbsence}
      />
    </Card>
  )
}
