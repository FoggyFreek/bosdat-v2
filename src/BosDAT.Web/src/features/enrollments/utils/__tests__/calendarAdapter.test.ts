import { describe, it, expect } from 'vitest'
import { createEnrollmentColorScheme, formatTimeSlotToTime } from '../calendarAdapter'
import type { TimeSlot } from '@/features/calendar/types'

describe('calendarAdapter', () => {
  describe('createEnrollmentColorScheme', () => {
    it('should return color scheme with all event types', () => {
      const colorScheme = createEnrollmentColorScheme()

      expect(colorScheme).toHaveProperty('individual')
      expect(colorScheme).toHaveProperty('group')
      expect(colorScheme).toHaveProperty('workshop')
      expect(colorScheme).toHaveProperty('trail')
      expect(colorScheme).toHaveProperty('holiday')
    })

    it('should have valid color properties for each type', () => {
      const colorScheme = createEnrollmentColorScheme()
      const eventTypes = ['individual', 'group', 'workshop', 'trail', 'holiday']

      eventTypes.forEach((eventType) => {
        expect(colorScheme[eventType]).toHaveProperty('background')
        expect(colorScheme[eventType]).toHaveProperty('border')
        expect(colorScheme[eventType]).toHaveProperty('textBackground')

        // Verify they are valid hex colors
        expect(colorScheme[eventType].background).toMatch(/^#[0-9a-f]{6}$/i)
        expect(colorScheme[eventType].border).toMatch(/^#[0-9a-f]{6}$/i)
        expect(colorScheme[eventType].textBackground).toMatch(/^#[0-9a-f]{6}$/i)
      })
    })

    it('should return consistent colors across multiple calls', () => {
      const scheme1 = createEnrollmentColorScheme()
      const scheme2 = createEnrollmentColorScheme()

      expect(scheme1).toEqual(scheme2)
    })
  })

  describe('formatTimeSlotToTime', () => {
    it('should format timeslot with hour and minute to HH:mm', () => {
      const timeslot: TimeSlot = {
        date: new Date('2024-01-15T09:30:00'),
        hour: 9,
        minute: 30,
      }

      const result = formatTimeSlotToTime(timeslot)
      expect(result).toBe('09:30')
    })

    it('should pad single digit hours and minutes with rounding', () => {
      const timeslot: TimeSlot = {
        date: new Date('2024-01-15T06:05:00'),
        hour: 6,
        minute: 5,
      }

      const result = formatTimeSlotToTime(timeslot)
      // 5 minutes rounds to nearest 10 = 10 minutes
      expect(result).toBe('06:10')
    })

    it('should handle midnight (00:00)', () => {
      const timeslot: TimeSlot = {
        date: new Date('2024-01-15T00:00:00'),
        hour: 0,
        minute: 0,
      }

      const result = formatTimeSlotToTime(timeslot)
      expect(result).toBe('00:00')
    })

    it('should handle noon (12:00)', () => {
      const timeslot: TimeSlot = {
        date: new Date('2024-01-15T12:00:00'),
        hour: 12,
        minute: 0,
      }

      const result = formatTimeSlotToTime(timeslot)
      expect(result).toBe('12:00')
    })

    it('should handle late evening with rounding', () => {
      const timeslot: TimeSlot = {
        date: new Date('2024-01-15T23:45:00'),
        hour: 23,
        minute: 45,
      }

      const result = formatTimeSlotToTime(timeslot)
      // 45 minutes rounds to nearest 10 = 50 minutes
      expect(result).toBe('23:50')
    })

    it('should round minutes to nearest 10-minute interval', () => {
      const testCases = [
        { minute: 0, expected: '09:00' },
        { minute: 3, expected: '09:00' },
        { minute: 7, expected: '09:10' },
        { minute: 12, expected: '09:10' },
        { minute: 18, expected: '09:20' },
        { minute: 23, expected: '09:20' },
        { minute: 27, expected: '09:30' },
        { minute: 33, expected: '09:30' },
        { minute: 38, expected: '09:40' },
        { minute: 42, expected: '09:40' },
        { minute: 47, expected: '09:50' },
        { minute: 52, expected: '09:50' },
        { minute: 57, expected: '10:00' },
      ]

      testCases.forEach(({ minute, expected }) => {
        const timeslot: TimeSlot = {
          date: new Date('2024-01-15T09:00:00'),
          hour: 9,
          minute,
        }

        const result = formatTimeSlotToTime(timeslot)
        expect(result).toBe(expected)
      })
    })
  })
})
