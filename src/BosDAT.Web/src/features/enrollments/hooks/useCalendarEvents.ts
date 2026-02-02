import { useMemo } from 'react'
import type { CalendarLesson, Holiday } from '@/features/schedule/types'
import type { Course } from '@/features/courses/types'
import type { CalendarEvent, EventType, EventFrequency } from '@/components/calendar/types'

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
}: UseCalendarEventsProps): CalendarEvent[] => {
  return useMemo(() => {
    const events: CalendarEvent[] = []
    const today = new Date().toISOString().split('T')[0]

    // Transform lessons to events
    const safeLessons = lessons || []
    safeLessons.forEach((lesson) => {
      const lessonDate = new Date(lesson.date)
      const startDateTime = combineDateAndTime(lessonDate, lesson.startTime)
      const endDateTime = combineDateAndTime(lessonDate, lesson.endTime)

      // Skip if we couldn't create valid date/times
      if (!startDateTime || !endDateTime) {
        return
      }

      events.push({
        startDateTime: startDateTime.toISOString(),
        endDateTime: endDateTime.toISOString(),
        title: lesson.title,
        frequency: isTrial ? 'once' : 'weekly',
        eventType: isTrial ? 'trail' : 'course',
        attendees: lesson.studentName ? [lesson.studentName] : [],
        room: undefined,
      })
    })

    // Transform courses to events (only in Course mode, not Trail mode)
    if (!isTrial) {
      const safeCourses = courses || []
      safeCourses.forEach((course) => {
        // Calculate the date for this course within the displayed week
        // weekStart is Monday, dayOfWeek is 0-6 (0=Sunday, 1=Monday, etc.)
        const offset = course.dayOfWeek === 0 ? 6 : course.dayOfWeek - 1
        const courseDate = new Date(weekStart)
        courseDate.setDate(weekStart.getDate() + offset)

        const startDateTime = combineDateAndTime(courseDate, course.startTime)
        const endDateTime = combineDateAndTime(courseDate, course.endTime)

        // Skip if we couldn't create valid date/times
        if (!startDateTime || !endDateTime) {
          return
        }

        // Skip future courses (courses that haven't started yet)
        const isFuture = course.startDate > today
        if (isFuture) return

        const studentNames = (course.enrollments || []).map((e) => e.studentName)
        const frequency: EventFrequency = course.frequency === 'Biweekly' ? 'bi-weekly' : 'weekly'

        events.push({
          startDateTime: startDateTime.toISOString(),
          endDateTime: endDateTime.toISOString(),
          title: `${course.courseTypeName} - ${course.teacherName}`,
          frequency,
          eventType: determineEventType(course.isWorkshop),
          attendees: studentNames,
          room: course.roomId?.toString(),
        })
      })
    }

    // Transform holidays to events
    const safeHolidays = holidays || []
    safeHolidays.forEach((holiday) => {
      // For each day within the holiday period that falls in the current week
      const holidayStart = new Date(holiday.startDate)
      const holidayEnd = new Date(holiday.endDate)
      const weekEnd = new Date(weekStart)
      weekEnd.setDate(weekStart.getDate() + 6)

      // Find overlapping days
      const overlapStart = new Date(Math.max(holidayStart.getTime(), weekStart.getTime()))
      const overlapEnd = new Date(Math.min(holidayEnd.getTime(), weekEnd.getTime()))

      for (let d = new Date(overlapStart); d <= overlapEnd; d.setDate(d.getDate() + 1)) {
        const dayStart = new Date(d)
        dayStart.setHours(0, 0, 0, 0)
        const dayEnd = new Date(d)
        dayEnd.setHours(23, 59, 59, 999)

        events.push({
          startDateTime: dayStart.toISOString(),
          endDateTime: dayEnd.toISOString(),
          title: holiday.name,
          frequency: 'once',
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
 * Determines the event type based on workshop flag
 */
const determineEventType = (isWorkshop: boolean): EventType => {
  return isWorkshop ? 'workshop' : 'course'
}

/**
 * Validates a time string in HH:mm format
 */
const isValidTime = (time: string | null | undefined): boolean => {
  if (!time || typeof time !== 'string') return false

  const parts = time.split(':')
  if (parts.length !== 2) return false

  const [hours, minutes] = parts.map(Number)
  if (isNaN(hours) || isNaN(minutes)) return false
  if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59) return false

  return true
}

/**
 * Validates a date string or Date object
 */
const isValidDate = (date: string | Date | null | undefined): boolean => {
  if (!date) return false

  const dateObj = typeof date === 'string' ? new Date(date) : date
  return dateObj instanceof Date && !isNaN(dateObj.getTime())
}

/**
 * Combines a date and time string into a Date object
 * Uses local time to ensure events display at the correct time regardless of timezone
 * Returns null if inputs are invalid
 */
const combineDateAndTime = (date: Date, time: string): Date | null => {
  // Validate inputs
  if (!isValidTime(time) || !isValidDate(date)) {
    return null
  }

  const [hours, minutes] = time.split(':').map(Number)
  const result = new Date(date)
  result.setHours(hours, minutes, 0, 0)

  // Verify the result is valid
  if (isNaN(result.getTime())) {
    return null
  }

  return result
}
