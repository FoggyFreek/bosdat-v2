import { useQuery } from '@tanstack/react-query'
import { courseTypesApi, teachersApi } from '@/services/api'
import { SummaryCard, type SummaryItem } from '@/components/SummaryCard'
import type { CourseType } from '@/features/course-types/types'
import type { TeacherList } from '@/features/teachers/types'
import { useEnrollmentForm } from '../context/EnrollmentFormContext'
import { getDayNameFromNumber } from '@/lib/datetime-helpers'
import { RecurrenceType } from '../types'

const RECURRENCE_LABELS: Record<RecurrenceType, string> = {
  Trail: 'Trial Lesson',
  Weekly: 'Once per week',
  Biweekly: 'Every two weeks',
}

const formatDate = (date: string | null) => {
  if (!date) return '-'
  return new Date(date).toLocaleDateString('nl-NL')
}

export const Step2Summary = () => {
  const { formData } = useEnrollmentForm()
  const { step1 } = formData

  const { data: courseTypes = [] } = useQuery<CourseType[]>({
    queryKey: ['courseTypes', 'active'],
    queryFn: () => courseTypesApi.getAll({ activeOnly: true }),
  })

  const { data: teachers = [] } = useQuery<TeacherList[]>({
    queryKey: ['teachers', 'active'],
    queryFn: () => teachersApi.getAll({ activeOnly: true }),
  })

  const selectedCourseType = courseTypes.find((ct) => ct.id === step1.courseTypeId)
  const selectedTeacher = teachers.find((t) => t.id === step1.teacherId)

  const dayOfWeek = step1.startDate
    ? getDayNameFromNumber(new Date(step1.startDate).getDay())
    : null

  const items: SummaryItem[] = [
    {
      label: 'Course Type:',
      value: (
        <>
          {selectedCourseType?.name || '-'}{' '}
          {selectedCourseType?.type && (
            <span className="text-muted-foreground">({selectedCourseType.type})</span>
          )}
        </>
      ),
    },
    {
      label: 'Teacher:',
      value: selectedTeacher?.fullName || '-',
    },
    {
      label: 'Start Date:',
      value: (
        <>
          {formatDate(step1.startDate)} {dayOfWeek && `(${dayOfWeek})`}
        </>
      ),
    },
    ...(step1.endDate
      ? [{ label: 'End Date:', value: formatDate(step1.endDate) }]
      : []),
    ...(step1.isTrial
      ? [{ label: 'Type:', value: <span className="text-amber-600">Trial Lesson</span> }]
      : []),
    {
      label: 'Recurrence:',
      value: RECURRENCE_LABELS[step1.recurrence],
    },
    ...(selectedCourseType
      ? [{ label: 'Max Students:', value: String(selectedCourseType.maxStudents) }]
      : []),
  ]

  return <SummaryCard title="Lesson Configuration" items={items} />
}
