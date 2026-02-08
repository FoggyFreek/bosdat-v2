import { useQuery } from '@tanstack/react-query'
import { courseTypesApi } from '@/features/course-types/api'
import { teachersApi } from '@/features/teachers/api'
import { EnrollmentSummaryCard } from './EnrollmentSummaryCard'
import type { CourseType } from '@/features/course-types/types'
import type { TeacherList } from '@/features/teachers/types'
import { useEnrollmentForm } from '../context/EnrollmentFormContext'
import { getDayNameFromNumber } from '@/lib/datetime-helpers'
import type { RecurrenceType } from '../types'

const RECURRENCE_LABELS: Record<RecurrenceType, string> = {
  Trial: 'Once',
  Weekly: 'Once per week',
  Biweekly: 'Every two weeks',
}

const formatDate = (date: string | null) => {
  if (!date) return undefined
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
    : undefined

  return (
    <EnrollmentSummaryCard
      title="Lesson Configuration"
      courseTypeName={selectedCourseType?.name}
      courseTypeLabel={selectedCourseType?.type}
      teacherName={selectedTeacher?.fullName}
      startDate={formatDate(step1.startDate)}
      dayOfWeek={dayOfWeek ?? undefined}
      endDate={formatDate(step1.endDate)}
      isTrial={step1.recurrence === 'Trial'}
      frequency={RECURRENCE_LABELS[step1.recurrence]}
      maxStudents={selectedCourseType?.maxStudents}
    />
  )
}
