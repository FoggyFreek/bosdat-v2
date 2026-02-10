import { useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { useCourseDetailData } from '@/features/courses/hooks/useCourseDetailData'
import type { LessonFilters } from '@/features/courses/hooks/useCourseDetailData'
import { getStatusColor } from '@/features/courses/hooks/useCoursesData'
import { CourseSummaryCard } from '@/features/courses/components/CourseSummaryCard'
import { CourseEnrollmentsCard } from '@/features/courses/components/CourseEnrollmentsCard'
import { CourseLessonHistoryCard } from '@/features/courses/components/CourseLessonHistoryCard'
import { CancelLessonDialog } from '@/features/lessons/components/CancelLessonDialog'
import type { Lesson } from '@/features/lessons/types'

export function CourseDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [lessonFilters, setLessonFilters] = useState<LessonFilters>({})
  const [cancelLesson, setCancelLesson] = useState<Lesson | null>(null)
  const { course, lessons, isLoading, isError } = useCourseDetailData(id!, lessonFilters)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  if (isError || !course) {
    return (
      <div className="text-center py-8">
        <p className="text-muted-foreground">Course not found</p>
        <Button asChild className="mt-4">
          <Link to="/courses">Back to Courses</Link>
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild>
          <Link to="/courses">
            <ArrowLeft className="h-5 w-5" />
          </Link>
        </Button>
        <div className="flex-1">
          <h1 className="text-3xl font-bold">
            {course.instrumentName} – {course.courseTypeName}
          </h1>
          <span
            className={cn(
              'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
              getStatusColor(course.status)
            )}
          >
            {course.status}
          </span>
        </div>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <CourseSummaryCard course={course} />
        <CourseEnrollmentsCard enrollments={course.enrollments ?? []} />
      </div>

      <CourseLessonHistoryCard
        lessons={lessons}
        courseId={id!}
        filters={lessonFilters}
        onFiltersChange={setLessonFilters}
        onCancelLesson={setCancelLesson}
      />

      <CancelLessonDialog
        lesson={cancelLesson}
        courseId={id!}
        onClose={() => setCancelLesson(null)}
      />
    </div>
  )
}
