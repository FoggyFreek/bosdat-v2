import { useQuery } from '@tanstack/react-query'
import { coursesApi } from '@/features/courses/api'
import { lessonsApi } from '@/features/lessons/api'
import type { Course } from '@/features/courses/types'
import type { Lesson } from '@/features/lessons/types'

export interface LessonFilters {
  startDate?: string
  endDate?: string
}

export const useCourseDetailData = (courseId: string, filters?: LessonFilters) => {
  const hasDateFilters = !!filters?.startDate || !!filters?.endDate

  const {
    data: course,
    isLoading: isCourseLoading,
    isError: isCourseError,
  } = useQuery<Course>({
    queryKey: ['course', courseId],
    queryFn: () => coursesApi.getById(courseId),
    enabled: !!courseId,
  })

  const {
    data: lessons,
    isLoading: isLessonsLoading,
    isError: isLessonsError,
  } = useQuery<Lesson[]>({
    queryKey: ['course', courseId, 'lessons', filters?.startDate, filters?.endDate],
    queryFn: () =>
      lessonsApi.getAll({
        courseId,
        startDate: filters?.startDate,
        endDate: filters?.endDate,
        ...(hasDateFilters ? {} : { top: 10 }),
      }),
    enabled: !!courseId,
  })

  return {
    course,
    lessons: lessons ?? [],
    isLoading: isCourseLoading || isLessonsLoading,
    isError: isCourseError || isLessonsError,
  }
}
