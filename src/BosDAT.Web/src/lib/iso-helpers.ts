/**
 * ISO 8601 date/time utilities for consistent date handling across the application.
 * Uses native JavaScript Date methods for all operations.
 */

// ============================================================================
// ISO DateTime String Creation
// ============================================================================

/**
 * Creates an ISO 8601 datetime string from date and time components.
 * Supports time formats: 'HH:mm:ss' and 'HH:mm:ss:ff' (fractional seconds).
 * Uses native Date methods for parsing and formatting.
 *
 * @param date - Date in MM-dd-yyyy format (e.g., "02-15-2026")
 * @param time - Time in HH:mm:ss or HH:mm:ss:ff format (e.g., "19:30:00" or "19:30:00:00")
 * @returns ISO 8601 formatted datetime string (e.g., "2026-02-15T19:30:00Z")
 * @throws Error if date or time format is invalid
 */
export const createIsoDateTime = (date: string, time: string): string => {
  // Parse MM-dd-yyyy format
  const [month, day, year] = date.split('-').map(Number)

  if (!month || !day || !year || month < 1 || month > 12 || day < 1 || day > 31) {
    throw new Error(`Invalid date format: '${date}'. Expected format: MM-dd-yyyy`)
  }

  // Parse HH:mm:ss or HH:mm:ss:ff format
  const timeParts = time.split(':').map(Number)

  if (timeParts.length < 3 || timeParts.length > 4) {
    throw new Error(`Invalid time format: '${time}'. Expected format: HH:mm:ss or HH:mm:ss:ff`)
  }

  const [hours, minutes, seconds] = timeParts
  // Fractional seconds (if present) are ignored - we only support second precision

  if (
    hours < 0 ||
    hours > 23 ||
    minutes < 0 ||
    minutes > 59 ||
    seconds < 0 ||
    seconds > 59
  ) {
    throw new Error(`Invalid time values in: '${time}'`)
  }

  // Create Date in UTC using Date.UTC
  const dateTime = new Date(Date.UTC(year, month - 1, day, hours, minutes, seconds))

  // Format as ISO 8601 with 'Z' suffix
  return dateTime.toISOString().replace(/\.\d{3}Z$/, 'Z')
}

/**
 * Creates an ISO 8601 datetime string from a Date object and time string.
 *
 * @param date - JavaScript Date object
 * @param time - Time in HH:mm:ss or HH:mm:ss:ff format
 * @returns ISO 8601 formatted datetime string
 */
export const createIsoDateTimeFromDate = (date: Date, time: string): string => {
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  const year = date.getFullYear()

  return createIsoDateTime(`${month}-${day}-${year}`, time)
}

/**
 * Combines a Date and time string into an ISO 8601 datetime string.
 *
 * @param date - JavaScript Date object
 * @param time - Time in HH:mm format
 * @returns ISO 8601 formatted datetime string
 */
export const combineDateAndTime = (date: Date, time: string): string => {
  const [hours, minutes] = time.split(':').map(Number)

  if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59) {
    throw new Error(`Invalid time format: '${time}'. Expected HH:mm`)
  }

  // Use Date.UTC to create UTC datetime
  const utcDate = new Date(
    Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), hours, minutes, 0)
  )

  return utcDate.toISOString().replace(/\.\d{3}Z$/, 'Z')
}

// ============================================================================
// Date Conversions
// ============================================================================

/**
 * Formats a Date as YYYY-MM-DD for API calls.
 * Uses native toISOString and splits on 'T'.
 *
 * @param date - JavaScript Date object
 * @returns Date string in YYYY-MM-DD format
 */
export const formatDateForApi = (date: Date): string => {
  return date.toISOString().split('T')[0]
}

/**
 * Gets today's date as YYYY-MM-DD in UTC.
 *
 * @returns Today's date in YYYY-MM-DD format
 */
export const getTodayForApi = (): string => {
  return new Date().toISOString().split('T')[0]
}

/**
 * Formats a date for display using nl-NL locale.
 *
 * @param date - Date string or Date object
 * @returns Formatted date string (e.g., "20 mrt 2024")
 */
export const formatDate = (date: string | Date): string => {
  return new Date(date).toLocaleDateString('nl-NL', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

/**
 * Formats a Date object to a local ISO string without timezone offset.
 * @param date 
 * @returns 
 */
export function toLocalISOString(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  const seconds = String(date.getSeconds()).padStart(2, '0');
  
  return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
}


/**
 * Extracts the date portion from an ISO datetime string.
 *
 * @param dateTimeString - ISO 8601 datetime string
 * @returns Date object representing the date
 */
export const getDateFromDateTime = (dateTimeString: string): Date => {
  return new Date(dateTimeString);
}

// ============================================================================
// Time Conversions
// ============================================================================

/**
 * Formats a time string to HH:mm format.
 *
 * @param time - Time string (may include seconds)
 * @returns Time in HH:mm format
 */
export const formatTime = (time: string): string => {
  return time.substring(0, 5)
}

/**
 * Converts time string (HH:mm) to minutes from midnight.
 *
 * @param time - Time in HH:mm format
 * @returns Minutes from midnight
 */
export const timeToMinutes = (time: string): number => {
  const [hours, minutes] = time.split(':').map(Number)
  return hours * 60 + minutes
}

/**
 * Converts minutes from midnight to time string (HH:mm).
 *
 * @param minutes - Minutes from midnight
 * @returns Time in HH:mm format
 */
export const minutesToTime = (minutes: number): string => {
  const hours = Math.floor(minutes / 60) % 24
  const mins = minutes % 60
  return `${String(hours).padStart(2, '0')}:${String(mins).padStart(2, '0')}`
}

/**
 * Calculates end time by adding duration (in minutes) to start time.
 *
 * @param startTime - Start time in HH:mm format
 * @param durationMinutes - Duration in minutes
 * @returns End time in HH:mm format
 */
export const calculateEndTime = (startTime: string, durationMinutes: number): string => {
  const startMinutes = timeToMinutes(startTime)
  const endMinutes = startMinutes + durationMinutes
  return minutesToTime(endMinutes)
}

/**
 * Calculates the start time as decimal hours from an ISO datetime string.
 *
 * @param dateTimeString - ISO 8601 datetime string
 * @returns Hours as decimal (e.g., 9.5 for 9:30 AM)
 */
export const getDecimalHours = (dateTimeString: string): number => {
  const date = new Date(dateTimeString)
  const hours = date.getHours()
  const minutes = date.getMinutes()
  return hours + minutes / 60
}

/**
 * Returns the hours component of a time string.
 *
 * @param dateTimeString - ISO 8601 datetime string
 * @returns Hours as number (e.g., 9 for 9:30 AM)
 */
export const getHoursFromTimeString = (dateTimeString: string): number => {
  // Add a dummy date to make it a valid ISO string
  const date = new Date(`1970-01-01T${dateTimeString}`);
  return date.getHours()
}

/**
 * Returns the minutes component of a time string.
 *
 * @param dateTimeString - ISO 8601 datetime string
 * @returns Minutes as number (e.g., 30 for 9:30 AM)
 */
export const getMinutesFromTimeString = (dateTimeString: string): number => {
  // Add a dummy date to make it a valid ISO string
  const date = new Date(`1970-01-01T${dateTimeString}`);
  return date.getMinutes()
}

/**
 * Calculates duration in decimal hours between two datetime strings.
 *
 * @param startDateTime - ISO 8601 datetime string
 * @param endDateTime - ISO 8601 datetime string
 * @returns Duration in hours as decimal (e.g., 1.5 for 90 minutes)
 */
export const getDurationInHours = (startDateTime: string, endDateTime: string): number => {
  const start = new Date(startDateTime)
  const end = new Date(endDateTime)
  const durationMs = end.getTime() - start.getTime()
  return durationMs / (1000 * 60 * 60)
}

/**
 * Formats datetime range to display time string.
 *
 * @param startDateTime - ISO 8601 datetime string
 * @param endDateTime - ISO 8601 datetime string
 * @returns Formatted time string (e.g., "09:30 – 10:00")
 */
export const formatTimeRange = (startDateTime: string, endDateTime: string): string => {
  const start = new Date(startDateTime)
  const end = new Date(endDateTime)

  const formatTime = (date: Date) => {
    const hours = date.getHours().toString().padStart(2, '0')
    const minutes = date.getMinutes().toString().padStart(2, '0')
    return `${hours}:${minutes}`
  }

  return `${formatTime(start)} – ${formatTime(end)}`
}

// ============================================================================
// Week Operations (ISO 8601)
// ============================================================================

/**
 * Week parity for biweekly course scheduling.
 * Matches backend WeekParity enum.
 */
export type WeekParity = 'All' | 'Odd' | 'Even'

/**
 * Gets the ISO 8601 week number for a given date.
 * Uses the algorithm: Week 1 is the week containing January 4th.
 * Matches backend IsoDateHelper.GetIsoWeekNumber().
 *
 * @param date - JavaScript Date object
 * @returns ISO week number (1-53)
 */
export const getIsoWeekNumber = (date: Date): number => {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()))
  // Set to nearest Thursday: current date + 4 - current day number (Monday = 1)
  const dayNum = d.getUTCDay() || 7
  d.setUTCDate(d.getUTCDate() + 4 - dayNum)
  // Get first day of year
  const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1))
  // Calculate full weeks to nearest Thursday
  const weekNumber = Math.ceil((((d.getTime() - yearStart.getTime()) / 86400000) + 1) / 7)
  return weekNumber
}

/**
 * Gets the week parity (Odd or Even) for a given date based on its ISO week number.
 * Matches backend IsoDateHelper.GetWeekParity().
 *
 * @param date - JavaScript Date object
 * @returns 'Odd' for odd weeks (1, 3, 5, ...), 'Even' for even weeks (2, 4, 6, ...)
 */
export const getWeekParity = (date: Date): 'Odd' | 'Even' => {
  const weekNumber = getIsoWeekNumber(date)
  return weekNumber % 2 === 1 ? 'Odd' : 'Even'
}

/**
 * Checks if a date matches a given week parity.
 * Matches backend IsoDateHelper.MatchesWeekParity().
 *
 * @param date - JavaScript Date object
 * @param parity - The week parity to match against
 * @returns True if the date matches the parity, false otherwise. 'All' always returns true.
 */
export const matchesWeekParity = (date: Date, parity: WeekParity): boolean => {
  if (parity === 'All') {
    return true
  }
  return getWeekParity(date) === parity
}

/**
 * Gets the Monday of the week containing the given date (ISO 8601 week).
 * Uses native Date methods.
 *
 * @param date - JavaScript Date object
 * @returns Date object representing Monday of the week
 */
export const getWeekStart = (date: Date): Date => {
  const d = new Date(date);
  const day = d.getDay();
  // Adjust to Monday: Sunday(0) moves back 6, others move back (day - 1)
  const diff = d.getDate() - (day === 0 ? 6 : day - 1);
  d.setDate(diff);
  d.setHours(12, 0, 0, 0); //set hour to noon to avoid timezone issues
  return d;
}

/**
 * Gets an array of 7 days starting from Monday of the given week.
 *
 * @param weekStart - Date object representing Monday
 * @returns Array of 7 Date objects (Monday through Sunday)
 */
export const getWeekDays = (weekStart: Date): Date[] => {
  const days: Date[] = []
  for (let i = 0; i < 7; i++) {
    const day = new Date(weekStart)
    day.setDate(weekStart.getDate() + i)
    days.push(day)
  }
  return days
}

// ============================================================================
// Day Operations
// ============================================================================

/**
 * Maps day names to JavaScript Date.getDay() values.
 * Sunday = 0, Monday = 1, ... Saturday = 6
 */
export const DAY_NAME_TO_NUMBER = {
  Sunday: 0,
  Monday: 1,
  Tuesday: 2,
  Wednesday: 3,
  Thursday: 4,
  Friday: 5,
  Saturday: 6,
} as const

export type DayOfWeek = keyof typeof DAY_NAME_TO_NUMBER

/**
 * Converts a day name to its JavaScript Date.getDay() number.
 *
 * @param day - Day name (e.g., "Monday")
 * @returns Day number (0=Sunday, 1=Monday, ..., 6=Saturday)
 */
export const dayNameToNumber = (day: DayOfWeek): number => DAY_NAME_TO_NUMBER[day]

/**
 * Gets the full day name from a Date object.
 * Uses native toLocaleDateString.
 *
 * @param date - JavaScript Date object
 * @returns Full day name (e.g., "Monday")
 */
export const getDayName = (date: Date): string => {
  return date.toLocaleDateString('en-US', { weekday: 'long' })
}

/**
 * Gets the full day name from a day number (0-6).
 *
 * @param day - Day number (0 = Sunday, 6 = Saturday)
 * @returns Full day name
 */
export const getDayNameFromNumber = (day: number): string => {
  const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
  return days[day] || ''
}

/**
 * Checks if two dates represent the same calendar day.
 *
 * @param date1 - First date to compare
 * @param date2 - Second date to compare
 * @returns True if both dates are the same calendar day
 */
export const isSameDay = (date1: Date, date2: Date): boolean => {
  return (
    date1.getFullYear() === date2.getFullYear() &&
    date1.getMonth() === date2.getMonth() &&
    date1.getDate() === date2.getDate()
  )
}

// ============================================================================
// Validation
// ============================================================================

/**
 * Validates that an event has valid time bounds.
 *
 * @param startDateTime - ISO 8601 datetime string
 * @param endDateTime - ISO 8601 datetime string
 * @returns True if the event times are valid (end is after start)
 */
export const isValidEventTime = (startDateTime: string, endDateTime: string): boolean => {
  const start = new Date(startDateTime)
  const end = new Date(endDateTime)
  return end.getTime() > start.getTime()
}
