/**
 * Calendar utility functions for date and time manipulation
 *
 * @deprecated This file is deprecated. All functions now re-export from iso-helpers.ts
 * Please import directly from '@/lib/iso-helpers' instead.
 */

// Re-export all calendar-related functions from iso-helpers for backward compatibility
export {
  getWeekStart,
  getWeekDays,
  formatDateForApi,
  timeToMinutes,
  minutesToTime,
  calculateEndTime,
  getDayName,
} from './iso-helpers'
