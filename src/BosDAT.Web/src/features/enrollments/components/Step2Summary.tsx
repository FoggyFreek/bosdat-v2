import { useQuery } from '@tanstack/react-query'
import { courseTypesApi, teachersApi } from '@/services/api'
import type { CourseType } from '@/features/course-types/types'
import type { TeacherList } from '@/features/teachers/types'
import { useEnrollmentForm } from '../context/EnrollmentFormContext'
import { getDayName } from '@/lib/utils'
import { RecurrenceType } from '../types'

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
    ? getDayName(new Date(step1.startDate).getDay())
    : null

  const getRecurrenceLabel = (recurrence: RecurrenceType) => {
  const labels: Record<RecurrenceType, string> = {
    Trail: 'Trial Lesson',
    Weekly: 'Once per week',
    Biweekly: 'Every two weeks',
  }
  return labels[recurrence]
}
  const formatDate = (date: string | null) => {
    if (!date) return '-'
    return new Date(date).toLocaleDateString('nl-NL')
  }

  return (
    <div className="rounded-lg border bg-muted/50 p-4">
      <h3 className="font-medium mb-3 text-sm">Lesson Configuration</h3>
      <div className="grid gap-2 text-sm">
        <div className="flex justify-between">
          <span className="text-muted-foreground">Course Type:</span>
          <span className="font-medium">
            {selectedCourseType?.name || '-'}{' '}
            {selectedCourseType?.type && (
              <span className="text-muted-foreground">({selectedCourseType.type})</span>
            )}
          </span>
        </div>
        <div className="flex justify-between">
          <span className="text-muted-foreground">Teacher:</span>
          <span className="font-medium">{selectedTeacher?.fullName || '-'}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-muted-foreground">Start Date:</span>
          <span className="font-medium">
            {formatDate(step1.startDate)} {dayOfWeek && `(${dayOfWeek})`}
          </span>
        </div>
        {step1.endDate && (
          <div className="flex justify-between">
            <span className="text-muted-foreground">End Date:</span>
            <span className="font-medium">{formatDate(step1.endDate)}</span>
          </div>
        )}
        {step1.isTrial && (
          <div className="flex justify-between">
            <span className="text-muted-foreground">Type:</span>
            <span className="font-medium text-amber-600">Trial Lesson</span>
          </div>
        )}
        <div className="flex justify-between">
          <span className="text-muted-foreground">Recurrence:</span>
          <span className="font-medium">
            {getRecurrenceLabel(step1.recurrence)}
          </span>
        </div>
        {selectedCourseType && (
          <div className="flex justify-between">
            <span className="text-muted-foreground">Max Students:</span>
            <span className="font-medium">{selectedCourseType.maxStudents}</span>
          </div>
        )}
      </div>
    </div>
  )
}
