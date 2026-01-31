import type { ColorScheme, TimeSlot } from '@/components/calendar/types'

/**
 * Creates the color scheme for enrollment calendar events
 * Maps event types to colors matching the enrollment feature design
 * @returns ColorScheme object for CalendarComponent
 */
export const createEnrollmentColorScheme = (): ColorScheme => {
  return {
    course: {
      background: '#eff6ff',
      border: '#3b82f6',
      textBackground: '#dbeafe',
    },
    workshop: {
      background: '#f0fdf4',
      border: '#22c55e',
      textBackground: '#dcfce7',
    },
    trail: {
      background: '#fef3c7',
      border: '#f59e0b',
      textBackground: '#fde68a',
    },
    holiday: {
      background: '#fef2f2',
      border: '#ef4444',
      textBackground: '#fee2e2',
    },
    absence: {
      background: '#fefce8',
      border: '#ca8a04',
      textBackground: '#fef08a',
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
 * Pads a number with leading zero if single digit
 * @param num - Number to pad
 * @returns Padded string
 */
const padZero = (num: number): string => {
  return num.toString().padStart(2, '0')
}
