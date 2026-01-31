import { useMemo } from 'react'
import type { CalendarLesson, Holiday } from '@/features/schedule/types'
import type { Course } from '@/features/courses/types'
import type { Event } from '@/features/calendar/types'

interface UseCalendarEventsProps {
  weekStart: Date
  lessons: CalendarLesson[]
  courses: Course[]
  holidays: Holiday[]
  isTrial: boolean
}

/**
 * Transforms API data (lessons, courses, holidays) directly into calendar Event[]
 * Eliminates the intermediate CalendarGridItem transformation step
 */
export const useCalendarEvents = ({
  weekStart,
  lessons,
  courses,
  holidays,
  isTrial,
}: UseCalendarEventsProps): Event[] => {
  return useMemo(() => {
    const events: Event[] = []
    const today = new Date().toISOString().split('T')[0]

    // Transform lessons to events
    lessons.forEach((lesson) => {
      const lessonDate = new Date(lesson.date)
      const startDateTime = combineDateAndTime(lessonDate, lesson.startTime)
      const endDateTime = combineDateAndTime(lessonDate, lesson.endTime)

      events.push({
        startDateTime: startDateTime.toISOString(),
        endDateTime: endDateTime.toISOString(),
        title: lesson.title,
        frequency: isTrial ? 'Trail' : 'Weekly',
        eventType: isTrial ? 'trail' : determineEventType(lesson.instrumentName, false),
        attendees: lesson.studentName ? [lesson.studentName] : [],
        room: undefined,
      })
    })

    // Transform courses to events (only in Course mode, not Trail mode)
    if (!isTrial) {
      courses.forEach((course) => {
        // Calculate the date for this course within the displayed week
        // weekStart is Monday, dayOfWeek is 0-6 (0=Sunday, 1=Monday, etc.)
        const offset = course.dayOfWeek === 0 ? 6 : course.dayOfWeek - 1
        const courseDate = new Date(weekStart)
        courseDate.setDate(weekStart.getDate() + offset)

        const startDateTime = combineDateAndTime(courseDate, course.startTime)
        const endDateTime = combineDateAndTime(courseDate, course.endTime)

        // Skip future courses (courses that haven't started yet)
        const isFuture = course.startDate > today
        if (isFuture) return

        const studentNames = course.enrollments.map((e) => e.studentName)
        const frequency = course.frequency === 'Weekly' || course.frequency === 'Biweekly'
          ? course.frequency
          : 'Weekly'

        events.push({
          startDateTime: startDateTime.toISOString(),
          endDateTime: endDateTime.toISOString(),
          title: `${course.courseTypeName} - ${course.teacherName}`,
          frequency,
          eventType: determineEventType(course.courseTypeName, course.isWorkshop),
          attendees: studentNames,
          room: course.roomId?.toString(),
        })
      })
    }

    // Transform holidays to events
    holidays.forEach((holiday) => {
      // For each day within the holiday period that falls in the current week
      const holidayStart = new Date(holiday.startDate)
      const holidayEnd = new Date(holiday.endDate)
      const weekEnd = new Date(weekStart)
      weekEnd.setDate(weekStart.getDate() + 6)

      // Find overlapping days
      const overlapStart = holidayStart > weekStart ? holidayStart : weekStart
      const overlapEnd = holidayEnd < weekEnd ? holidayEnd : weekEnd

      for (let d = new Date(overlapStart); d <= overlapEnd; d.setDate(d.getDate() + 1)) {
        const dayStart = new Date(d)
        dayStart.setHours(0, 0, 0, 0)
        const dayEnd = new Date(d)
        dayEnd.setHours(23, 59, 59, 999)

        events.push({
          startDateTime: dayStart.toISOString(),
          endDateTime: dayEnd.toISOString(),
          title: holiday.name,
          frequency: 'Once',
          eventType: 'holiday',
          attendees: [],
        })
      }
    })

    // Sort by start time
    return events.toSorted((a, b) => a.startDateTime.localeCompare(b.startDateTime))
  }, [weekStart, lessons, courses, holidays, isTrial])
}

/**
 * Determines the event type based on course/lesson name and workshop flag
 */
const determineEventType = (name: string, isWorkshop: boolean): string => {
  if (isWorkshop) return 'workshop'

  const lowercaseName = name.toLowerCase()
  if (lowercaseName.includes('individual')) return 'individual'
  if (lowercaseName.includes('group')) return 'group'

  return 'individual'
}

/**
 * Combines a date and time string into a Date object
 * Uses local time to ensure events display at the correct time regardless of timezone
 */
const combineDateAndTime = (date: Date, time: string): Date => {
  const [hours, minutes] = time.split(':').map(Number)
  const result = new Date(date)
  result.setHours(hours, minutes, 0, 0)
  return result
}
