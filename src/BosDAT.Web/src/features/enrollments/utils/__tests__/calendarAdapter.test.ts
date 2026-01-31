import { describe, it, expect } from 'vitest'
import {
  transformGridItemsToEvents,
  createEnrollmentColorScheme,
  formatTimeSlotToTime,
} from '../calendarAdapter'
import type { CalendarGridItem } from '../../types'
import type { TimeSlot } from '@/features/calendar/types'

describe('calendarAdapter', () => {
  describe('transformGridItemsToEvents', () => {
    it('should transform empty array to empty array', () => {
      const result = transformGridItemsToEvents([])
      expect(result).toEqual([])
    })

    it('should transform single grid item to event', () => {
      const gridItems: CalendarGridItem[] = [
        {
          id: '1',
          type: 'course',
          courseType: 'Individual',
          title: 'Piano Lesson',
          date: '2024-01-15',
          startTime: '09:30',
          endTime: '10:00',
          teacherName: 'John Doe',
          studentNames: ['Alice Smith'],
          frequency: 'Weekly',
          isFuture: true,
          roomId: 1,
        },
      ]

      const result = transformGridItemsToEvents(gridItems)

      expect(result).toHaveLength(1)
      expect(result[0]).toMatchObject({
        startDateTime: '2024-01-15T09:30:00.000Z',
        endDateTime: '2024-01-15T10:00:00.000Z',
        title: 'Piano Lesson',
        frequency: 'Weekly',
        eventType: 'individual',
        attendees: ['Alice Smith'],
        room: '1',
      })
    })

    it('should handle multiple grid items on same date', () => {
      const gridItems: CalendarGridItem[] = [
        {
          id: '1',
          type: 'course',
          courseType: 'Individual',
          title: 'Piano',
          date: '2024-01-15',
          startTime: '09:00',
          endTime: '09:30',
          teacherName: 'Teacher A',
          studentNames: ['Student A'],
          frequency: 'Weekly',
          isFuture: true,
        },
        {
          id: '2',
          type: 'course',
          courseType: 'Group',
          title: 'Guitar',
          date: '2024-01-15',
          startTime: '10:00',
          endTime: '11:00',
          teacherName: 'Teacher B',
          studentNames: ['Student B', 'Student C'],
          frequency: 'Biweekly',
          isFuture: true,
        },
      ]

      const result = transformGridItemsToEvents(gridItems)

      expect(result).toHaveLength(2)
      expect(result[0].title).toBe('Piano')
      expect(result[1].title).toBe('Guitar')
      expect(result[1].attendees).toEqual(['Student B', 'Student C'])
    })

    it('should handle events across multiple dates in a week', () => {
      const gridItems: CalendarGridItem[] = [
        {
          id: '1',
          type: 'lesson',
          courseType: 'Individual',
          title: 'Monday Lesson',
          date: '2024-01-15',
          startTime: '09:00',
          endTime: '10:00',
          teacherName: 'Teacher A',
          studentNames: ['Student A'],
          frequency: 'Weekly',
          isFuture: true,
        },
        {
          id: '2',
          type: 'course',
          courseType: 'Group',
          title: 'Wednesday Course',
          date: '2024-01-17',
          startTime: '14:00',
          endTime: '15:00',
          teacherName: 'Teacher B',
          studentNames: ['Student B', 'Student C'],
          frequency: 'Weekly',
          isFuture: true,
        },
        {
          id: '3',
          type: 'lesson',
          courseType: 'Workshop',
          title: 'Friday Workshop',
          date: '2024-01-19',
          startTime: '16:00',
          endTime: '18:00',
          teacherName: 'Teacher C',
          studentNames: ['Student D', 'Student E', 'Student F'],
          frequency: 'Weekly',
          isFuture: true,
        },
      ]

      const result = transformGridItemsToEvents(gridItems)

      expect(result).toHaveLength(3)

      // Verify Monday event
      expect(result[0].startDateTime).toBe('2024-01-15T09:00:00.000Z')
      expect(result[0].endDateTime).toBe('2024-01-15T10:00:00.000Z')
      expect(result[0].title).toBe('Monday Lesson')

      // Verify Wednesday event
      expect(result[1].startDateTime).toBe('2024-01-17T14:00:00.000Z')
      expect(result[1].endDateTime).toBe('2024-01-17T15:00:00.000Z')
      expect(result[1].title).toBe('Wednesday Course')

      // Verify Friday event
      expect(result[2].startDateTime).toBe('2024-01-19T16:00:00.000Z')
      expect(result[2].endDateTime).toBe('2024-01-19T18:00:00.000Z')
      expect(result[2].title).toBe('Friday Workshop')
    })

    it('should map all course types correctly', () => {
      const courseTypes: Array<'Individual' | 'Group' | 'Workshop' | 'Trail'> = [
        'Individual',
        'Group',
        'Workshop',
        'Trail',
      ]

      courseTypes.forEach((courseType) => {
        const gridItem: CalendarGridItem = {
          id: '1',
          type: 'course',
          courseType,
          title: `${courseType} Course`,
          date: '2024-01-15',
          startTime: '09:00',
          endTime: '10:00',
          teacherName: 'Teacher',
          studentNames: ['Student'],
          isFuture: true,
        }

        const result = transformGridItemsToEvents([gridItem])
        expect(result[0].eventType).toBe(courseType.toLowerCase())
      })
    })

    it('should handle grid items without room', () => {
      const gridItem: CalendarGridItem = {
        id: '1',
        type: 'lesson',
        courseType: 'Individual',
        title: 'Trial Lesson',
        date: '2024-01-15',
        startTime: '14:00',
        endTime: '14:30',
        teacherName: 'Teacher',
        studentNames: ['Student'],
        frequency: 'Trail',
        isFuture: true,
      }

      const result = transformGridItemsToEvents([gridItem])
      expect(result[0].room).toBeUndefined()
    })

    it('should handle empty student names array', () => {
      const gridItem: CalendarGridItem = {
        id: '1',
        type: 'course',
        courseType: 'Group',
        title: 'Empty Group',
        date: '2024-01-15',
        startTime: '15:00',
        endTime: '16:00',
        teacherName: 'Teacher',
        studentNames: [],
        isFuture: true,
      }

      const result = transformGridItemsToEvents([gridItem])
      expect(result[0].attendees).toEqual([])
    })

    it('should handle times at midnight boundary', () => {
      const gridItem: CalendarGridItem = {
        id: '1',
        type: 'course',
        courseType: 'Individual',
        title: 'Late Night',
        date: '2024-01-15',
        startTime: '23:30',
        endTime: '00:00',
        teacherName: 'Teacher',
        studentNames: ['Student'],
        isFuture: true,
      }

      const result = transformGridItemsToEvents([gridItem])
      expect(result[0].startDateTime).toBe('2024-01-15T23:30:00.000Z')
      // Note: endTime 00:00 will be on the same day in this implementation
      expect(result[0].endDateTime).toBe('2024-01-15T00:00:00.000Z')
    })

    it('should use Weekly as default frequency when not provided', () => {
      const gridItem: CalendarGridItem = {
        id: '1',
        type: 'course',
        courseType: 'Individual',
        title: 'No Frequency',
        date: '2024-01-15',
        startTime: '10:00',
        endTime: '11:00',
        teacherName: 'Teacher',
        studentNames: ['Student'],
        isFuture: true,
      }

      const result = transformGridItemsToEvents([gridItem])
      expect(result[0].frequency).toBe('Weekly')
    })

    it('should handle grid items without date field (fallback to current date)', () => {
      const gridItem: CalendarGridItem = {
        id: '1',
        type: 'course',
        courseType: 'Individual',
        title: 'No Date',
        startTime: '10:00',
        endTime: '11:00',
        teacherName: 'Teacher',
        studentNames: ['Student'],
        isFuture: true,
      }

      const result = transformGridItemsToEvents([gridItem])
      expect(result).toHaveLength(1)
      // Should create a valid ISO string even without date
      expect(result[0].startDateTime).toMatch(/^\d{4}-\d{2}-\d{2}T10:00:00\.\d{3}Z$/)
    })
  })

  describe('createEnrollmentColorScheme', () => {
    it('should return color scheme with all course types', () => {
      const colorScheme = createEnrollmentColorScheme()

      expect(colorScheme).toHaveProperty('individual')
      expect(colorScheme).toHaveProperty('group')
      expect(colorScheme).toHaveProperty('workshop')
      expect(colorScheme).toHaveProperty('trail')
    })

    it('should have valid color properties for each type', () => {
      const colorScheme = createEnrollmentColorScheme()
      const eventTypes = ['individual', 'group', 'workshop', 'trail']

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
