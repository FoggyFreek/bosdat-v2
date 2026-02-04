import { useMemo } from 'react'
import type { Course, CourseFrequency } from '@/features/courses/types'
import type { CalendarEvent, EventType, EventFrequency } from '@/components/calendar/types'
import { dayNameToNumber, matchesWeekParity, type WeekParity } from '@/lib/datetime-helpers'

interface UseCalendarEventsProps {
  weekStart: Date
  courses: Course[]
}

/**
 * Transforms active courses into calendar events for the enrollment slot selection.
 * Filters courses based on whether they occur in the given week, considering:
 * - Course frequency (Weekly, Biweekly, Monthly)
 * - Week parity for biweekly courses (Odd/Even weeks based on ISO 8601)
 * - Course start/end dates
 */
export const useCalendarEvents = ({
  weekStart,
  courses,
}: UseCalendarEventsProps): CalendarEvent[] => {
  return useMemo(() => {
    const events: CalendarEvent[] = []
    const safeCourses = courses ?? []

    safeCourses.forEach((course) => {
      // Check if course should be shown in current week
      if (!shouldShowCourseInWeek(course, weekStart)) {
        return
      }
      // Calculate the date for this course within the displayed week
      const courseDate = getCourseDate(weekStart, dayNameToNumber(course.dayOfWeek))
      const startDateTime = combineDateAndTime(courseDate, course.startTime)
      const endDateTime = combineDateAndTime(courseDate, course.endTime)

      // Skip if we couldn't create valid date/times
      if (!startDateTime || !endDateTime) {
        return
      }

      const studentNames = (course.enrollments ?? []).map((e) => e.studentName)
      const frequency = mapFrequency(course.frequency)

      events.push({
        id: course.id,
        startDateTime: startDateTime.toISOString(),
        endDateTime: endDateTime.toISOString(),
        title: `${course.courseTypeName} - ${course.teacherName}`,
        frequency,
        eventType: determineEventType(course.isWorkshop),
        attendees: studentNames,
        room: course.roomId?.toString(),
      })
    })

    // Sort by start time
    return events.toSorted((a, b) => a.startDateTime.localeCompare(b.startDateTime))
  }, [weekStart, courses])
}

/**
 * Determines if a course should be shown in the given week based on:
 * - Course has started (startDate <= week end)
 * - Course hasn't ended (no endDate or endDate >= week start)
 * - Frequency/parity match for biweekly courses
 */
const shouldShowCourseInWeek = (course: Course, weekStart: Date): boolean => {
  const weekEnd = new Date(weekStart)
  weekEnd.setDate(weekStart.getDate() + 6)

  // Check if course has started (course startDate should be <= week end)
  const courseStartDate = new Date(course.startDate)
  if (courseStartDate > weekEnd) {
    return false
  }

  // Check if course hasn't ended (no endDate or endDate >= week start)
  if (course.endDate) {
    const courseEndDate = new Date(course.endDate)
    if (courseEndDate < weekStart) {
      return false
    }
  }

  // Check frequency-based visibility
  return matchesCourseFrequency(course.frequency, course.weekParity, weekStart)
}

/**
 * Checks if a course should appear based on its frequency and the current week.
 * - Weekly: Always shows
 * - Biweekly: Shows only in weeks matching the course's weekParity
 * - Monthly: Shows only in the first week of each month (simplified logic)
 */
const matchesCourseFrequency = (
  frequency: CourseFrequency,
  weekParity: WeekParity,
  weekStart: Date
): boolean => {
  switch (frequency) {
    case 'Weekly':
      return true

    case 'Biweekly':
      // For biweekly courses, check if the week's parity matches the course's weekParity
      return matchesWeekParity(weekStart, weekParity)

    case 'Monthly':
      // Monthly courses: simplified logic - show if any day in the week is in the first 7 days of a month
      // This is a simplified approach; actual monthly logic may vary based on business rules
      for (let i = 0; i < 7; i++) {
        const day = new Date(weekStart)
        day.setDate(weekStart.getDate() + i)
        if (day.getDate() <= 7) {
          return true
        }
      }
      return false

    default:
      return true
  }
}

/**
 * Calculates the date for a course within the given week.
 * weekStart is Monday, dayOfWeek uses JS convention (0=Sunday, 1=Monday, etc.)
 */
const getCourseDate = (weekStart: Date, dayOfWeek: number): Date => {
  // Convert dayOfWeek to offset from Monday
  // Monday=1 -> offset 0, Tuesday=2 -> offset 1, ..., Sunday=0 -> offset 6
  const offset = dayOfWeek === 0 ? 6 : dayOfWeek - 1
  const courseDate = new Date(weekStart)
  courseDate.setDate(weekStart.getDate() + offset)
  return courseDate
}

/**
 * Determines the event type based on workshop flag
 */
const determineEventType = (isWorkshop: boolean): EventType => {
  return isWorkshop ? 'workshop' : 'course'
}

/**
 * Maps course frequency to calendar event frequency
 */
const mapFrequency = (frequency: CourseFrequency): EventFrequency => {
  switch (frequency) {
    case 'Biweekly':
      return 'bi-weekly'
    case 'Weekly':
    case 'Monthly':
    default:
      return 'weekly'
  }
}

/**
 * Validates a time string in HH:mm format
 */
const isValidTime = (time: string | null | undefined): boolean => {
  if (!time || typeof time !== 'string') return false

  const parts = time.split(':')
  if (parts.length < 2) return false

  const [hours, minutes] = parts.map(Number)
  if (isNaN(hours) || isNaN(minutes)) return false
  if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59) return false

  return true
}

/**
 * Combines a date and time string into a Date object
 * Uses local time to ensure events display at the correct time regardless of timezone
 * Returns null if inputs are invalid
 */
const combineDateAndTime = (date: Date, time: string): Date | null => {
  if (!isValidTime(time)) {
    return null
  }

  const [hours, minutes] = time.split(':').map(Number)
  const result = new Date(date)
  result.setHours(hours, minutes, 0, 0)

  if (isNaN(result.getTime())) {
    return null
  }

  return result
}
