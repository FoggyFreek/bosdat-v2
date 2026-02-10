import { useState, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { lessonsApi } from '@/features/lessons/api'
import { CancelLessonDialog } from '@/features/lessons/components/CancelLessonDialog'
import { CourseLessonHistoryCard } from '@/features/courses/components/CourseLessonHistoryCard'
import type { Lesson } from '@/features/lessons/types'
import type { LessonFilters } from '@/features/courses/hooks/useCourseDetailData'

interface LessonsSectionProps {
  studentId: string
}

export function LessonsSection({ studentId }: LessonsSectionProps) {
  const [filters, setFilters] = useState<LessonFilters>({})
  const [cancelLesson, setCancelLesson] = useState<Lesson | null>(null)

  const hasDateFilters = !!filters.startDate || !!filters.endDate

  const { data: lessons = [] } = useQuery<Lesson[]>({
    queryKey: ['lessons', 'student', studentId, filters.startDate, filters.endDate],
    queryFn: () => lessonsApi.getByStudent(studentId),
    enabled: !!studentId,
  })

  const filteredLessons = useMemo(() => {
    let result = [...lessons]

    if (filters.startDate) {
      result = result.filter((l) => l.scheduledDate >= filters.startDate!)
    }
    if (filters.endDate) {
      result = result.filter((l) => l.scheduledDate <= filters.endDate!)
    }

    result.sort((a, b) => {
      const dateA = new Date(a.scheduledDate)
      const dateB = new Date(b.scheduledDate)
      return dateB.getTime() - dateA.getTime()
    })

    if (!hasDateFilters) {
      return result.slice(0, 10)
    }

    return result
  }, [lessons, filters.startDate, filters.endDate, hasDateFilters])

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">Lessons</h2>

      <CourseLessonHistoryCard
        lessons={filteredLessons}
        filters={filters}
        onFiltersChange={setFilters}
        onCancelLesson={setCancelLesson}
        showCourseName
      />

      {cancelLesson && (
        <CancelLessonDialog
          lesson={cancelLesson}
          courseId={cancelLesson.courseId}
          onClose={() => setCancelLesson(null)}
        />
      )}
    </div>
  )
}
