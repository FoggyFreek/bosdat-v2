import { describe, it, expect } from 'vitest'
import {
  createIsoDateTime,
  createIsoDateTimeFromDate,
  combineDateAndTime,
  formatDateForApi,
  getTodayForApi,
  formatDate,
  getDateFromDateTime,
  formatTime,
  timeToMinutes,
  minutesToTime,
  calculateEndTime,
  getDecimalHours,
  getDurationInHours,
  formatTimeRange,
  getWeekStart,
  getWeekDays,
  getDayName,
  getDayNameFromNumber,
  isSameDay,
  isValidEventTime,
} from '../iso-helpers'

describe('iso-helpers', () => {
  // ============================================================================
  // ISO DateTime String Creation
  // ============================================================================

  describe('createIsoDateTime', () => {
    it('should create ISO datetime string from MM-dd-yyyy and HH:mm:ss', () => {
      const result = createIsoDateTime('02-15-2026', '19:30:00')
      expect(result).toBe('2026-02-15T19:30:00Z')
    })

    it('should create ISO datetime string from MM-dd-yyyy and HH:mm:ss:ff (fractional seconds)', () => {
      const result = createIsoDateTime('02-15-2026', '19:30:00:00')
      expect(result).toBe('2026-02-15T19:30:00Z')
    })

    it('should handle midnight', () => {
      const result = createIsoDateTime('01-01-2024', '00:00:00')
      expect(result).toBe('2024-01-01T00:00:00Z')
    })

    it('should handle end of day', () => {
      const result = createIsoDateTime('12-31-2025', '23:59:59')
      expect(result).toBe('2025-12-31T23:59:59Z')
    })

    it('should handle various times with fractional seconds', () => {
      expect(createIsoDateTime('03-20-2024', '09:15:30')).toBe('2024-03-20T09:15:30Z')
      expect(createIsoDateTime('03-20-2024', '09:15:30:50')).toBe('2024-03-20T09:15:30Z')
    })

    it('should throw error for invalid date format', () => {
      expect(() => createIsoDateTime('2026-02-15', '19:30:00')).toThrow('Invalid date format')
      expect(() => createIsoDateTime('15-02-2026', '19:30:00')).toThrow('Invalid date format')
    })

    it('should throw error for invalid time format', () => {
      expect(() => createIsoDateTime('02-15-2026', '19:30')).toThrow('Invalid time format')
      expect(() => createIsoDateTime('02-15-2026', '7:30 PM')).toThrow('Invalid time format')
    })

    it('should throw error for invalid date values', () => {
      expect(() => createIsoDateTime('13-15-2026', '19:30:00')).toThrow('Invalid date format')
      expect(() => createIsoDateTime('02-32-2026', '19:30:00')).toThrow('Invalid date format')
    })

    it('should throw error for invalid time values', () => {
      expect(() => createIsoDateTime('02-15-2026', '25:30:00')).toThrow('Invalid time values')
      expect(() => createIsoDateTime('02-15-2026', '19:60:00')).toThrow('Invalid time values')
      expect(() => createIsoDateTime('02-15-2026', '19:30:60')).toThrow('Invalid time values')
    })
  })

  describe('createIsoDateTimeFromDate', () => {
    it('should create ISO datetime from Date object and time string', () => {
      const date = new Date(2026, 1, 15) // February 15, 2026
      const result = createIsoDateTimeFromDate(date, '19:30:00')
      expect(result).toBe('2026-02-15T19:30:00Z')
    })

    it('should support fractional seconds', () => {
      const date = new Date(2026, 1, 15)
      const result = createIsoDateTimeFromDate(date, '19:30:00:00')
      expect(result).toBe('2026-02-15T19:30:00Z')
    })
  })

  describe('combineDateAndTime', () => {
    it('should combine date and HH:mm time into ISO datetime', () => {
      const date = new Date(2026, 1, 15) // February 15, 2026
      const result = combineDateAndTime(date, '19:30')
      expect(result).toBe('2026-02-15T19:30:00Z')
    })

    it('should handle midnight', () => {
      const date = new Date(2024, 0, 1)
      const result = combineDateAndTime(date, '00:00')
      expect(result).toBe('2024-01-01T00:00:00Z')
    })

    it('should throw error for invalid time format', () => {
      const date = new Date(2026, 1, 15)
      expect(() => combineDateAndTime(date, '25:00')).toThrow('Invalid time format')
      expect(() => combineDateAndTime(date, '19:60')).toThrow('Invalid time format')
    })
  })

  // ============================================================================
  // Date Conversions
  // ============================================================================

  describe('formatDateForApi', () => {
    it('should format date as YYYY-MM-DD using native toISOString', () => {
      const date = new Date(Date.UTC(2024, 2, 20, 15, 30)) // March 20, 2024 UTC
      const formatted = formatDateForApi(date)
      expect(formatted).toBe('2024-03-20')
    })

    it('should pad single-digit months and days', () => {
      const date = new Date(Date.UTC(2024, 2, 5)) // March 5, 2024 UTC
      const formatted = formatDateForApi(date)
      expect(formatted).toBe('2024-03-05')
    })
  })

  describe('getTodayForApi', () => {
    it('should return today in YYYY-MM-DD format', () => {
      const result = getTodayForApi()
      expect(result).toMatch(/^\d{4}-\d{2}-\d{2}$/)
    })
  })

  describe('formatDate', () => {
    it('should format date using nl-NL locale', () => {
      const date = new Date(2024, 2, 20) // March 20, 2024
      const result = formatDate(date)
      expect(result).toMatch(/20/)
      expect(result).toMatch(/mrt/)
      expect(result).toMatch(/2024/)
    })

    it('should accept string dates', () => {
      const result = formatDate('2024-03-20')
      expect(result).toMatch(/20/)
      expect(result).toMatch(/mrt/)
      expect(result).toMatch(/2024/)
    })
  })

  describe('getDateFromDateTime', () => {
    it('should extract date from ISO datetime string', () => {
      const result = getDateFromDateTime('2026-02-15T19:30:00Z')
      expect(result).toBeInstanceOf(Date)
      expect(result.getUTCFullYear()).toBe(2026)
      expect(result.getUTCMonth()).toBe(1) // February
      expect(result.getUTCDate()).toBe(15)
    })

    it('should handle datetime without Z suffix', () => {
      const result = getDateFromDateTime('2024-03-20T09:15:30')
      expect(result).toBeInstanceOf(Date)
      expect(result.getFullYear()).toBe(2024)
      expect(result.getMonth()).toBe(2) // March
      expect(result.getDate()).toBe(20)
    })
  })

  // ============================================================================
  // Time Conversions
  // ============================================================================

  describe('formatTime', () => {
    it('should extract HH:mm from time string', () => {
      expect(formatTime('19:30:00')).toBe('19:30')
      expect(formatTime('09:15:45')).toBe('09:15')
    })

    it('should handle HH:mm format', () => {
      expect(formatTime('19:30')).toBe('19:30')
    })
  })

  describe('timeToMinutes', () => {
    it('should convert time to minutes from midnight', () => {
      expect(timeToMinutes('00:00')).toBe(0)
      expect(timeToMinutes('01:00')).toBe(60)
      expect(timeToMinutes('12:30')).toBe(750)
      expect(timeToMinutes('23:59')).toBe(1439)
    })

    it('should handle single-digit hours and minutes', () => {
      expect(timeToMinutes('09:05')).toBe(545)
    })
  })

  describe('minutesToTime', () => {
    it('should convert minutes from midnight to time', () => {
      expect(minutesToTime(0)).toBe('00:00')
      expect(minutesToTime(60)).toBe('01:00')
      expect(minutesToTime(750)).toBe('12:30')
      expect(minutesToTime(1439)).toBe('23:59')
    })

    it('should pad single-digit hours and minutes', () => {
      expect(minutesToTime(545)).toBe('09:05')
    })

    it('should handle time crossing midnight', () => {
      expect(minutesToTime(1440)).toBe('00:00') // 24 hours = midnight next day
      expect(minutesToTime(1455)).toBe('00:15')
    })
  })

  describe('calculateEndTime', () => {
    it('should calculate end time by adding duration to start time', () => {
      expect(calculateEndTime('14:00', 60)).toBe('15:00')
      expect(calculateEndTime('14:30', 45)).toBe('15:15')
      expect(calculateEndTime('09:00', 90)).toBe('10:30')
    })

    it('should handle time crossing midnight', () => {
      expect(calculateEndTime('23:30', 45)).toBe('00:15')
    })

    it('should handle multiple-hour durations', () => {
      expect(calculateEndTime('10:00', 180)).toBe('13:00')
    })
  })

  describe('getDecimalHours', () => {
    it('should calculate decimal hours from ISO datetime', () => {
      expect(getDecimalHours('2024-03-20T09:00:00Z')).toBe(9)
      expect(getDecimalHours('2024-03-20T09:30:00Z')).toBe(9.5)
      expect(getDecimalHours('2024-03-20T14:15:00Z')).toBe(14.25)
    })

    it('should handle midnight', () => {
      expect(getDecimalHours('2024-03-20T00:00:00Z')).toBe(0)
    })
  })

  describe('getDurationInHours', () => {
    it('should calculate duration in hours between two datetimes', () => {
      const start = '2024-03-20T09:00:00Z'
      const end = '2024-03-20T10:30:00Z'
      expect(getDurationInHours(start, end)).toBe(1.5)
    })

    it('should handle whole hour durations', () => {
      const start = '2024-03-20T09:00:00Z'
      const end = '2024-03-20T12:00:00Z'
      expect(getDurationInHours(start, end)).toBe(3)
    })

    it('should handle cross-day durations', () => {
      const start = '2024-03-20T23:00:00Z'
      const end = '2024-03-21T01:00:00Z'
      expect(getDurationInHours(start, end)).toBe(2)
    })
  })

  describe('formatTimeRange', () => {
    it('should format datetime range as time string', () => {
      const start = '2024-03-20T09:30:00Z'
      const end = '2024-03-20T10:00:00Z'
      expect(formatTimeRange(start, end)).toBe('09:30 – 10:00')
    })

    it('should pad single-digit hours', () => {
      const start = '2024-03-20T09:00:00Z'
      const end = '2024-03-20T09:45:00Z'
      expect(formatTimeRange(start, end)).toBe('09:00 – 09:45')
    })
  })

  // ============================================================================
  // Week Operations
  // ============================================================================

  describe('getWeekStart', () => {
    it('should return Monday for a date in the middle of the week', () => {
      const wednesday = new Date(2024, 2, 20) // March 20, 2024 (Wednesday)
      const weekStart = getWeekStart(wednesday)

      expect(weekStart.getDay()).toBe(1) // Monday is day 1
      expect(weekStart.getFullYear()).toBe(2024)
      expect(weekStart.getMonth()).toBe(2) // March (0-indexed)
      expect(weekStart.getDate()).toBe(18)
    })

    it('should return the same date if already Monday', () => {
      const monday = new Date(2024, 2, 18) // March 18, 2024 (Monday)
      const weekStart = getWeekStart(monday)

      expect(weekStart.getFullYear()).toBe(2024)
      expect(weekStart.getMonth()).toBe(2)
      expect(weekStart.getDate()).toBe(18)
    })

    it('should return Monday for Sunday', () => {
      const sunday = new Date(2024, 2, 24) // March 24, 2024 (Sunday)
      const weekStart = getWeekStart(sunday)

      expect(weekStart.getDay()).toBe(1) // Monday
      expect(weekStart.getFullYear()).toBe(2024)
      expect(weekStart.getMonth()).toBe(2)
      expect(weekStart.getDate()).toBe(18)
    })

    it('should set time to noon to avoid timezone issues', () => {
      const wednesday = new Date(2024, 2, 20, 15, 30, 45) // March 20, 2024, 15:30:45
      const weekStart = getWeekStart(wednesday)

      expect(weekStart.getHours()).toBe(12)
      expect(weekStart.getMinutes()).toBe(0)
      expect(weekStart.getSeconds()).toBe(0)
      expect(weekStart.getMilliseconds()).toBe(0)
    })
  })

  describe('getWeekDays', () => {
    it('should return 7 consecutive days starting from Monday', () => {
      const monday = new Date(2024, 2, 18) // March 18, 2024
      const weekDays = getWeekDays(monday)

      expect(weekDays).toHaveLength(7)
      expect(weekDays[0].getDate()).toBe(18) // Monday
      expect(weekDays[6].getDate()).toBe(24) // Sunday
    })

    it('should return correct day numbers', () => {
      const monday = new Date(2024, 2, 18)
      const weekDays = getWeekDays(monday)

      expect(weekDays[0].getDay()).toBe(1) // Monday
      expect(weekDays[1].getDay()).toBe(2) // Tuesday
      expect(weekDays[2].getDay()).toBe(3) // Wednesday
      expect(weekDays[3].getDay()).toBe(4) // Thursday
      expect(weekDays[4].getDay()).toBe(5) // Friday
      expect(weekDays[5].getDay()).toBe(6) // Saturday
      expect(weekDays[6].getDay()).toBe(0) // Sunday
    })
  })

  // ============================================================================
  // Day Operations
  // ============================================================================

  describe('getDayName', () => {
    it('should return full day name using native toLocaleDateString', () => {
      expect(getDayName(new Date(2024, 2, 18))).toBe('Monday')
      expect(getDayName(new Date(2024, 2, 19))).toBe('Tuesday')
      expect(getDayName(new Date(2024, 2, 20))).toBe('Wednesday')
      expect(getDayName(new Date(2024, 2, 21))).toBe('Thursday')
      expect(getDayName(new Date(2024, 2, 22))).toBe('Friday')
      expect(getDayName(new Date(2024, 2, 23))).toBe('Saturday')
      expect(getDayName(new Date(2024, 2, 24))).toBe('Sunday')
    })
  })

  describe('getDayNameFromNumber', () => {
    it('should return day name from day number', () => {
      expect(getDayNameFromNumber(0)).toBe('Sunday')
      expect(getDayNameFromNumber(1)).toBe('Monday')
      expect(getDayNameFromNumber(2)).toBe('Tuesday')
      expect(getDayNameFromNumber(3)).toBe('Wednesday')
      expect(getDayNameFromNumber(4)).toBe('Thursday')
      expect(getDayNameFromNumber(5)).toBe('Friday')
      expect(getDayNameFromNumber(6)).toBe('Saturday')
    })

    it('should return empty string for invalid day number', () => {
      expect(getDayNameFromNumber(-1)).toBe('')
      expect(getDayNameFromNumber(7)).toBe('')
      expect(getDayNameFromNumber(100)).toBe('')
    })
  })

  describe('isSameDay', () => {
    it('should return true for same calendar day', () => {
      const date1 = new Date(2024, 2, 20, 9, 0)
      const date2 = new Date(2024, 2, 20, 15, 30)
      expect(isSameDay(date1, date2)).toBe(true)
    })

    it('should return false for different days', () => {
      const date1 = new Date(2024, 2, 20)
      const date2 = new Date(2024, 2, 21)
      expect(isSameDay(date1, date2)).toBe(false)
    })

    it('should return false for same day different months', () => {
      const date1 = new Date(2024, 2, 20)
      const date2 = new Date(2024, 3, 20)
      expect(isSameDay(date1, date2)).toBe(false)
    })

    it('should return false for same day different years', () => {
      const date1 = new Date(2024, 2, 20)
      const date2 = new Date(2025, 2, 20)
      expect(isSameDay(date1, date2)).toBe(false)
    })
  })

  // ============================================================================
  // Validation
  // ============================================================================

  describe('isValidEventTime', () => {
    it('should return true when end is after start', () => {
      const start = '2024-03-20T09:00:00Z'
      const end = '2024-03-20T10:00:00Z'
      expect(isValidEventTime(start, end)).toBe(true)
    })

    it('should return false when end is before start', () => {
      const start = '2024-03-20T10:00:00Z'
      const end = '2024-03-20T09:00:00Z'
      expect(isValidEventTime(start, end)).toBe(false)
    })

    it('should return false when times are equal', () => {
      const time = '2024-03-20T09:00:00Z'
      expect(isValidEventTime(time, time)).toBe(false)
    })

    it('should handle cross-day events', () => {
      const start = '2024-03-20T23:00:00Z'
      const end = '2024-03-21T01:00:00Z'
      expect(isValidEventTime(start, end)).toBe(true)
    })
  })
})
