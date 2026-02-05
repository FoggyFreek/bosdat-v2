import { useQuery } from '@tanstack/react-query'
import { coursesApi, lessonsApi } from '@/services/api'
import type { Course } from '@/features/courses/types'
import type { Lesson } from '@/features/lessons/types'

export const useCourseDetailData = (courseId: string) => {
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
    queryKey: ['course', courseId, 'lessons'],
    queryFn: () => lessonsApi.getAll({ courseId, top: 10 }),
    enabled: !!courseId,
  })

  return {
    course,
    lessons: lessons ?? [],
    isLoading: isCourseLoading || isLessonsLoading,
    isError: isCourseError || isLessonsError,
  }
}
