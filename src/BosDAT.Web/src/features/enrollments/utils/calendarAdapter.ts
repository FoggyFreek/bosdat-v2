import type { CalendarGridItem } from '../types'
import type { Event, ColorScheme, TimeSlot } from '@/features/calendar/types'

/**
 * Transforms CalendarGridItem array to Event array for CalendarComponent
 * @param items - Array of calendar grid items from enrollment context
 * @returns Array of events in CalendarComponent format
 */
export const transformGridItemsToEvents = (
  items: CalendarGridItem[]
): Event[] => {
  return items.map((item) => {
    // Each item should have its own date, parse it
    const itemDate = item.date ? new Date(item.date) : new Date()
    const startDateTime = combineDateAndTime(itemDate, item.startTime)
    const endDateTime = combineDateAndTime(itemDate, item.endTime)

    return {
      startDateTime: startDateTime.toISOString(),
      endDateTime: endDateTime.toISOString(),
      title: item.title,
      frequency: item.frequency || 'Weekly',
      eventType: mapCourseTypeToEventType(item.courseType),
      attendees: item.studentNames,
      room: item.roomId?.toString(),
    }
  })
}

/**
 * Creates the color scheme for enrollment calendar events
 * Maps course types to colors matching the enrollment feature design
 * @returns ColorScheme object for CalendarComponent
 */
export const createEnrollmentColorScheme = (): ColorScheme => {
  return {
    individual: {
      background: '#eff6ff',
      border: '#3b82f6',
      textBackground: '#dbeafe',
    },
    group: {
      background: '#f0fdf4',
      border: '#22c55e',
      textBackground: '#dcfce7',
    },
    workshop: {
      background: '#fff7ed',
      border: '#f97316',
      textBackground: '#ffedd5',
    },
    trail: {
      background: '#fef3c7',
      border: '#f59e0b',
      textBackground: '#fde68a',
    },
  }
}

/**
 * Formats a TimeSlot object to HH:mm time string
 * Rounds minutes to nearest 10-minute interval for enrollment precision
 * @param timeslot - TimeSlot object from CalendarComponent click
 * @returns Time string in HH:mm format (e.g., "09:30")
 */
export const formatTimeSlotToTime = (timeslot: TimeSlot): string => {
  const { hour, minute } = timeslot

  // Round to nearest 10-minute interval
  const roundedMinute = Math.round(minute / 10) * 10

  // Handle overflow (e.g., 59 minutes rounds to 60)
  const finalHour = roundedMinute === 60 ? hour + 1 : hour
  const finalMinute = roundedMinute === 60 ? 0 : roundedMinute

  return `${padZero(finalHour)}:${padZero(finalMinute)}`
}

/**
 * Combines a date and time string into a Date object
 * @param date - The base date
 * @param time - Time string in HH:mm format
 * @returns Combined Date object
 */
const combineDateAndTime = (date: Date, time: string): Date => {
  const [hours, minutes] = time.split(':').map(Number)
  const result = new Date(date)
  result.setUTCHours(hours, minutes, 0, 0)
  return result
}

/**
 * Maps enrollment course type to calendar event type
 * @param courseType - Course type from enrollment domain
 * @returns Event type string (lowercase)
 */
const mapCourseTypeToEventType = (
  courseType: 'Individual' | 'Group' | 'Workshop' | 'Trail'
): string => {
  return courseType.toLowerCase()
}

/**
 * Pads a number with leading zero if single digit
 * @param num - Number to pad
 * @returns Padded string
 */
const padZero = (num: number): string => {
  return num.toString().padStart(2, '0')
}
