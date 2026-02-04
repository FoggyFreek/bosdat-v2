import { describe, it, expect } from 'vitest'
import {
  getWeekStart,
  getWeekDays,
  formatDateForApi,
  timeToMinutes,
  minutesToTime,
  calculateEndTime,
  getDayName,
} from '../datetime-helpers'

describe('calendar-utils', () => {
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

  describe('formatDateForApi', () => {
    it('should format date as YYYY-MM-DD', () => {
      const date = new Date(2024, 2, 20, 15, 30) // March 20, 2024, 15:30
      const formatted = formatDateForApi(date)

      expect(formatted).toMatch(/^\d{4}-\d{2}-\d{2}$/)
      expect(formatted).toBe('2024-03-20')
    })

    it('should pad single-digit months and days', () => {
      const date = new Date(Date.UTC(2024, 2, 5)) // March 5, 2024 UTC
      const formatted = formatDateForApi(date)

      expect(formatted).toBe('2024-03-05')
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

  describe('getDayName', () => {
    it('should return full day name', () => {
      expect(getDayName(new Date(2024, 2, 18))).toBe('Monday') // Monday
      expect(getDayName(new Date(2024, 2, 19))).toBe('Tuesday') // Tuesday
      expect(getDayName(new Date(2024, 2, 20))).toBe('Wednesday') // Wednesday
      expect(getDayName(new Date(2024, 2, 21))).toBe('Thursday') // Thursday
      expect(getDayName(new Date(2024, 2, 22))).toBe('Friday') // Friday
      expect(getDayName(new Date(2024, 2, 23))).toBe('Saturday') // Saturday
      expect(getDayName(new Date(2024, 2, 24))).toBe('Sunday') // Sunday
    })
  })
})
