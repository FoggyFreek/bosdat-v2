import { useQuery } from '@tanstack/react-query'
import { CheckCircle } from 'lucide-react'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { SummaryCard, type SummaryItem } from '@/components/SummaryCard'
import { useEnrollmentForm } from '../context/EnrollmentFormContext'
import { courseTypesApi } from '@/features/course-types/api'
import { teachersApi } from '@/features/teachers/api'
import { roomsApi } from '@/features/rooms/api'
import { getDayNameFromNumber, formatTime } from '@/lib/datetime-helpers'
import type { CourseType } from '@/features/course-types/types'
import type { TeacherList } from '@/features/teachers/types'
import type { Room } from '@/features/rooms/types'
import type { RecurrenceType } from '../types'

const RECURRENCE_LABELS: Record<RecurrenceType, string> = {
  Trail: 'Trial Lesson',
  Weekly: 'Once per week',
  Biweekly: 'Every two weeks',
}

const formatDate = (date: string | null) => {
  if (!date) return '-'
  return new Date(date).toLocaleDateString('nl-NL')
}

export function Step4Summary() {
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

  const dayOfWeek = step3.selectedDayOfWeek !== null
    ? getDayNameFromNumber(step3.selectedDayOfWeek)
    : null

  const courseItems: SummaryItem[] = [
    {
      label: 'Course Type:',
      value: (
        <>
          {selectedCourseType?.name ?? '-'}{' '}
          {selectedCourseType?.type && (
            <span className="text-muted-foreground">({selectedCourseType.type})</span>
          )}
        </>
      ),
    },
    {
      label: 'Teacher:',
      value: selectedTeacher?.fullName ?? '-',
    },
    {
      label: 'Start Date:',
      value: formatDate(step1.startDate),
    },
    ...(step1.endDate
      ? [{ label: 'End Date:', value: formatDate(step1.endDate) }]
      : []),
    {
      label: 'Frequency:',
      value: RECURRENCE_LABELS[step1.recurrence],
    },
    ...(step1.isTrial
      ? [{ label: 'Type:', value: <span className="text-amber-600">Trial Lesson</span> }]
      : []),
  ]

  const scheduleItems: SummaryItem[] = [
    { label: 'Day:', value: dayOfWeek ?? '-' },
    {
      label: 'Time:',
      value: step3.selectedStartTime && step3.selectedEndTime
        ? `${formatTime(step3.selectedStartTime)} – ${formatTime(step3.selectedEndTime)}`
        : '-',
    },
    { label: 'Room:', value: selectedRoom?.name ?? '-' },
  ]

  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-lg font-medium">Confirmation</h3>
        <p className="text-sm text-muted-foreground">
          Review your enrollment details before submitting
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <SummaryCard title="Course Details" items={courseItems} />
        <SummaryCard title="Schedule & Room" items={scheduleItems} />
      </div>

      <SummaryCard title={`Enrolled Students (${students.length})`}>
        {students.length === 0 && (
          <p className="text-sm text-muted-foreground">No students selected</p>
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
                    Starts {formatDate(student.enrolledAt)}
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
      </SummaryCard>

      <Alert className="border-green-200 bg-green-50">
        <CheckCircle className="h-4 w-4 text-green-600" />
        <AlertTitle className="text-green-900">
          Ready to submit
        </AlertTitle>
        <AlertDescription className="text-green-800">
          Your enrollment details have been configured. Conflict detection will be performed when you submit.
        </AlertDescription>
      </Alert>
    </div>
  )
}
