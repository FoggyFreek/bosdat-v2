import { useMemo } from 'react'
import type { CalendarLesson } from '@/features/schedule/types'
import type { Course } from '@/features/courses/types'
import type { CalendarGridItem } from '../types'

interface UseCalendarGridItemsProps {
  date: string
  weekStart: Date
  lessons: CalendarLesson[]
  courses: Course[]
  isTrial: boolean
}

const determineCourseType = (
  courseTypeName: string,
  isWorkshop: boolean
): 'Individual' | 'Group' | 'Workshop' => {
  if (isWorkshop) return 'Workshop'

  const lowercaseName = courseTypeName.toLowerCase()
  if (lowercaseName.includes('individual')) return 'Individual'
  if (lowercaseName.includes('group')) return 'Group'

  // Default to Individual if not specified
  return 'Individual'
}

/**
 * Transform API data (lessons and courses) into calendar grid items
 */
export const useCalendarGridItems = ({

  weekStart,
  lessons,
  courses,
  isTrial,
}: UseCalendarGridItemsProps): CalendarGridItem[] => {
  return useMemo(() => {
    const items: CalendarGridItem[] = []
    const today = new Date().toISOString().split('T')[0]

    // Transform lessons
    lessons.forEach((lesson) => {
      items.push({
        id: lesson.id,
        type: 'lesson',
        courseType: isTrial ? 'Trail' : determineCourseType(lesson.instrumentName, false),
        title: lesson.title,
        date: lesson.date,
        startTime: lesson.startTime,
        endTime: lesson.endTime,
        teacherName: lesson.teacherName,
        studentNames: lesson.studentName ? [lesson.studentName] : [],
        frequency: isTrial ? 'Trail' : undefined,
        isFuture: false,
      })
    })

    // Transform courses (only in Course mode, not Trail mode)
    if (!isTrial) {
      courses.forEach((course) => {
        const studentNames = course.enrollments.map((e) => e.studentName)

        // Calculate the date for this course within the displayed week
        // weekStart is Monday, dayOfWeek is 0-6 (0=Sunday, 1=Monday, etc.)
        // Offset: Monday(1)=0, Tuesday(2)=1, ..., Saturday(6)=5, Sunday(0)=6
        const offset = course.dayOfWeek === 0 ? 6 : course.dayOfWeek - 1
        const courseDate = new Date(weekStart)
        courseDate.setDate(weekStart.getDate() + offset)

        items.push({
          id: course.id,
          type: 'course',
          courseType: determineCourseType(course.courseTypeName, course.isWorkshop),
          title: `${course.courseTypeName} - ${course.teacherName}`,
          date: courseDate.toISOString().split('T')[0],
          startTime: course.startTime,
          endTime: course.endTime,
          teacherName: course.teacherName,
          studentNames,
          frequency: course.frequency === 'Weekly' || course.frequency === 'Biweekly'
            ? course.frequency
            : undefined,
          isFuture: course.startDate > today,
          roomId: course.roomId,
        })
      })
    }

    // Sort by start time
    return items.toSorted((a, b) => a.startTime.localeCompare(b.startTime))
  }, [weekStart, lessons, courses, isTrial])
}
