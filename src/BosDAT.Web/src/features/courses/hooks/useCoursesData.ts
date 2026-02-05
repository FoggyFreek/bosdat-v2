import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { coursesApi } from '@/services/api'
import { getDayNameFromNumber } from '@/lib/datetime-helpers'
import type { CourseList, CourseStatus } from '@/features/courses/types'

const DAYS_ORDERED = [1, 2, 3, 4, 5, 6, 0] as const

export interface DayGroup {
  day: number
  dayName: string
  courses: CourseList[]
}

const STATUS_COLORS: Record<CourseStatus, string> = {
  Active: 'bg-green-100 text-green-800',
  Paused: 'bg-yellow-100 text-yellow-800',
  Completed: 'bg-blue-100 text-blue-800',
  Cancelled: 'bg-gray-100 text-gray-800',
}

export const getStatusColor = (status: CourseStatus): string => {
  return STATUS_COLORS[status] ?? 'bg-gray-100 text-gray-800'
}

export const useCoursesData = () => {
  const { data, isLoading, isFetching, isError } = useQuery<CourseList[]>({
    queryKey: ['courses'],
    queryFn: () => coursesApi.getSummary(),
  })

  const courses = Array.isArray(data) ? data : []
  const showLoading = isLoading || (isFetching && courses.length === 0)

  const dayGroups = useMemo(() => {
    const coursesByDay = courses.reduce<Record<string, CourseList[]>>((acc, course) => {
      const dayName = course.dayOfWeek
      if (!acc[dayName]) {
        acc[dayName] = []
      }
      acc[dayName].push(course)
      return acc
    }, {})

    return DAYS_ORDERED
      .map((day) => {
        const dayName = getDayNameFromNumber(day)
        const dayCourses = coursesByDay[dayName] ?? []
        return { day, dayName, courses: dayCourses }
      })
      .filter((group) => group.courses.length > 0)
  }, [courses])

  return {
    courses,
    dayGroups,
    showLoading,
    isError,
  }
}
