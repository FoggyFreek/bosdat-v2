/**
 * Utility functions for date and time calculations
 *
 * @deprecated This file is deprecated. All functions now re-export from iso-helpers.ts
 * Please import directly from '@/lib/iso-helpers' instead.
 */

// Re-export all calendar utility functions from iso-helpers for backward compatibility
export {
  isSameDay,
  getDecimalHours,
  getDurationInHours,
  formatTimeRange,
  isValidEventTime,
} from '@/lib/iso-helpers'

/**
 * Extracts the date portion from an ISO datetime string as a Date object
 * @deprecated Use getDateFromDateTime from '@/lib/iso-helpers' which returns a string
 * @param dateTimeString - ISO 8601 datetime string
 * @returns Date object representing the date
 */
export const getDateFromDateTime = (dateTimeString: string): Date => {
  return new Date(dateTimeString)
}
