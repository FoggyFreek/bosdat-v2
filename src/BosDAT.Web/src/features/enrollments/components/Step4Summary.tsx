import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { CheckCircle } from 'lucide-react'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { EnrollmentSummaryCard } from './EnrollmentSummaryCard'
import { useEnrollmentForm } from '../context/EnrollmentFormContext'
import { courseTypesApi } from '@/features/course-types/api'
import { teachersApi } from '@/features/teachers/api'
import { roomsApi } from '@/features/rooms/api'
import { getDayNameFromNumber, formatTime } from '@/lib/datetime-helpers'
import type { CourseType } from '@/features/course-types/types'
import type { TeacherList } from '@/features/teachers/types'
import type { Room } from '@/features/rooms/types'
import type { RecurrenceType } from '../types'

const formatDate = (date: string | null) => {
  if (!date) return undefined
  return new Date(date).toLocaleDateString('nl-NL')
}

export function Step4Summary() {
  const { t } = useTranslation()
  const { formData } = useEnrollmentForm()
  const { step1, step2, step3 } = formData
  const students = step2.students ?? []

  const { data: courseTypes = [] } = useQuery<CourseType[]>({
    queryKey: ['courseTypes', 'active'],
    queryFn: () => courseTypesApi.getAll({ activeOnly: true }),
  })

  const { data: teachers = [] } = useQuery<TeacherList[]>({
    queryKey: ['teachers', 'active'],
    queryFn: () => teachersApi.getAll({ activeOnly: true }),
  })

  const { data: rooms = [] } = useQuery<Room[]>({
    queryKey: ['rooms', { activeOnly: true }],
    queryFn: () => roomsApi.getAll({ activeOnly: true }),
  })

  const selectedCourseType = courseTypes.find((ct) => ct.id === step1.courseTypeId)
  const selectedTeacher = teachers.find((t) => t.id === step1.teacherId)
  const selectedRoom = rooms.find((r) => r.id === step3.selectedRoomId)

  const dayOfWeek = step3.selectedDayOfWeek === null
    ? undefined
    : getDayNameFromNumber(step3.selectedDayOfWeek)

  const timeDisplay = step3.selectedStartTime && step3.selectedEndTime
    ? formatTime(step3.selectedStartTime)
    : undefined

  const endTimeDisplay = step3.selectedStartTime && step3.selectedEndTime
    ? formatTime(step3.selectedEndTime)
    : undefined

  const recurrenceLabels: Record<RecurrenceType, string> = {
    Trial: t('enrollments.step4.once'),
    Weekly: t('enrollments.step4.oncePerWeek'),
    Biweekly: t('enrollments.step4.everyTwoWeeks'),
  }

  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-lg font-medium">{t('enrollments.step4.confirmation')}</h3>
        <p className="text-sm text-muted-foreground">
          {t('enrollments.step4.reviewDetails')}
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <EnrollmentSummaryCard
          title={t('enrollments.step4.courseDetails')}
          courseTypeName={selectedCourseType?.name}
          courseTypeLabel={selectedCourseType?.type}
          teacherName={selectedTeacher?.fullName}
          startDate={formatDate(step1.startDate)}
          endDate={formatDate(step1.endDate)}
          frequency={recurrenceLabels[step1.recurrence]}
          isTrial={step1.recurrence === 'Trial'}
        />
        <EnrollmentSummaryCard
          title={t('enrollments.step4.scheduleAndRoom')}
          dayOfWeek={dayOfWeek}
          startTime={timeDisplay}
          endTime={endTimeDisplay}
          roomName={selectedRoom?.name}
        />
      </div>

      <EnrollmentSummaryCard title={t('enrollments.step4.enrolledStudents', { count: students.length })}>
        {students.length === 0 && (
          <p className="text-sm text-muted-foreground">{t('enrollments.step4.enrolledStudents')}</p>
        )}
        {students.length > 0 && (
          <div className="space-y-3">
            {students.map((student) => (
              <div
                key={student.studentId}
                className="flex items-center justify-between text-sm"
              >
                <div>
                  <p className="font-medium">{student.studentName}</p>
                  <p className="text-xs text-muted-foreground">
                    {t('enrollments.step4.starts', { date: formatDate(student.enrolledAt) ?? '-' })}
                    {student.discountPercentage > 0 && (
                      <> &middot; {student.discountPercentage}% discount ({student.discountType})</>
                    )}
                  </p>
                </div>
                <span className="text-xs text-muted-foreground">
                  {student.invoicingPreference}
                </span>
              </div>
            ))}
          </div>
        )}
      </EnrollmentSummaryCard>

      <Alert className="border-green-200 bg-green-50">
        <CheckCircle className="h-4 w-4 text-green-600" />
        <AlertTitle className="text-green-900">
          {t('enrollments.step4.readyToSubmit')}
        </AlertTitle>
        <AlertDescription className="text-green-800">
          {t('enrollments.step4.conflictCheckOnSubmit')}
        </AlertDescription>
      </Alert>
    </div>
  )
}
