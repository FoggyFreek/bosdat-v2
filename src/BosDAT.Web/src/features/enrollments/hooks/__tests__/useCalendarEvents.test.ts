import { describe, it, expect } from 'vitest'
import { renderHook } from '@testing-library/react'
import { useCalendarEvents } from '../useCalendarEvents'
import type { Course } from '@/features/courses/types'

describe('useCalendarEvents', () => {
  // Week 12 of 2024 starts on Monday March 18 (ISO week 12 is even)
  const weekStartEven = new Date('2024-03-18T12:00:00') // Even week (ISO week 12)
  // Week 11 of 2024 starts on Monday March 11 (ISO week 11 is odd)
  const weekStartOdd = new Date('2024-03-11T12:00:00') // Odd week (ISO week 11)

  const createMockCourse = (overrides: Partial<Course> = {}): Course => ({
    id: 'course-1',
    teacherId: 'teacher-1',
    teacherName: 'Jane Smith',
    courseTypeId: 1,
    courseTypeName: 'Individual Piano',
    instrumentName: 'Piano',
    roomId: 1,
    roomName: 'Room 1',
    dayOfWeek: 'Wednesday',
    startTime: '10:00',
    endTime: '11:00',
    frequency: 'Weekly',
    weekParity: 'All',
    startDate: '2024-01-01',
    status: 'Active',
    isWorkshop: false,
    isTrial: false,
    enrollmentCount: 1,
    enrollments: [],
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
    ...overrides,
  })

  describe('Course transformation', () => {
    it('should transform courses to events with calculated date and local time', () => {
      const courses = [createMockCourse({ dayOfWeek: 'Wednesday', startTime: '10:00', endTime: '11:00' })]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )

      expect(result.current).toHaveLength(1)

      // Verify the date is Wednesday (weekStart Monday + 2 days)
      const startDate = new Date(result.current[0].startDateTime)
      const endDate = new Date(result.current[0].endDateTime)

      expect(startDate.getDate()).toBe(20) // March 20 is Wednesday
      expect(startDate.getHours()).toBe(10)
      expect(startDate.getMinutes()).toBe(0)
      expect(endDate.getHours()).toBe(11)
      expect(endDate.getMinutes()).toBe(0)
    })

    it('should calculate correct dates for different days of week', () => {
      const courses = [
        createMockCourse({ id: 'course-1', dayOfWeek: 'Monday', startTime: '09:00' }),
        createMockCourse({ id: 'course-2', dayOfWeek: 'Friday', startTime: '11:00' }),
        createMockCourse({ id: 'course-3', dayOfWeek: 'Sunday', startTime: '13:00' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )

      expect(result.current[0].startDateTime).toContain('2024-03-18') // Monday
      expect(result.current[1].startDateTime).toContain('2024-03-22') // Friday
      expect(result.current[2].startDateTime).toContain('2024-03-24') // Sunday
    })

    it('should skip future courses (courses that have not started yet)', () => {
      const courses = [
        createMockCourse({ id: 'past', startDate: '2020-01-01' }),
        createMockCourse({ id: 'future', startDate: '2099-12-31' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )

      expect(result.current).toHaveLength(1)
      expect(result.current[0].id).toBe('past')
    })

    it('should skip courses that have ended', () => {
      const courses = [
        createMockCourse({ id: 'active', startDate: '2020-01-01', endDate: undefined }),
        createMockCourse({ id: 'ended', startDate: '2020-01-01', endDate: '2024-03-01' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )

      expect(result.current).toHaveLength(1)
      expect(result.current[0].id).toBe('active')
    })

    it('should determine eventType based on isWorkshop flag', () => {
      const courses = [
        createMockCourse({ id: 'c1', courseTypeName: 'Individual Piano', isWorkshop: false }),
        createMockCourse({ id: 'c2', courseTypeName: 'Group Guitar', isWorkshop: false }),
        createMockCourse({ id: 'c3', isWorkshop: true, courseTypeName: 'Drums Workshop' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )

      expect(result.current[0].eventType).toBe('course')
      expect(result.current[1].eventType).toBe('course')
      expect(result.current[2].eventType).toBe('workshop')
    })

    it('should extract student names from enrollments', () => {
      const courses = [
        createMockCourse({
          enrollments: [
            {
              id: 'e1',
              studentId: 's1',
              studentName: 'Bob Smith',
              courseId: 'course-1',
              enrolledAt: '2024-01-01',
              discountPercent: 0,
              discountType: 'None',
              status: 'Active',
            },
            {
              id: 'e2',
              studentId: 's2',
              studentName: 'Carol White',
              courseId: 'course-1',
              enrolledAt: '2024-01-01',
              discountPercent: 0,
              discountType: 'None',
              status: 'Active',
            },
          ],
        }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )

      expect(result.current[0].attendees).toEqual(['Bob Smith', 'Carol White'])
    })

    it('should include room as string', () => {
      const courses = [createMockCourse({ roomId: 42 })]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )

      expect(result.current[0].room).toBe('42')
    })

    it('should map frequency correctly', () => {
      const courses = [
        createMockCourse({ id: 'c1', frequency: 'Weekly' }),
        createMockCourse({ id: 'c2', frequency: 'Biweekly', weekParity: 'Even' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )

      expect(result.current[0].frequency).toBe('weekly')
      expect(result.current[1].frequency).toBe('bi-weekly')
    })
  })

  describe('Week parity filtering for biweekly courses', () => {
    it('should show biweekly course with Even parity in even weeks', () => {
      const courses = [
        createMockCourse({ id: 'even-course', frequency: 'Biweekly', weekParity: 'Even' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )

      expect(result.current).toHaveLength(1)
      expect(result.current[0].id).toBe('even-course')
    })

    it('should NOT show biweekly course with Even parity in odd weeks', () => {
      const courses = [
        createMockCourse({ id: 'even-course', frequency: 'Biweekly', weekParity: 'Even' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartOdd, courses })
      )

      expect(result.current).toHaveLength(0)
    })

    it('should show biweekly course with Odd parity in odd weeks', () => {
      const courses = [
        createMockCourse({ id: 'odd-course', frequency: 'Biweekly', weekParity: 'Odd' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartOdd, courses })
      )

      expect(result.current).toHaveLength(1)
      expect(result.current[0].id).toBe('odd-course')
    })

    it('should NOT show biweekly course with Odd parity in even weeks', () => {
      const courses = [
        createMockCourse({ id: 'odd-course', frequency: 'Biweekly', weekParity: 'Odd' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )

      expect(result.current).toHaveLength(0)
    })

    it('should always show biweekly course with All parity', () => {
      const courses = [
        createMockCourse({ id: 'all-course', frequency: 'Biweekly', weekParity: 'All' }),
      ]

      const { result: evenResult } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )
      const { result: oddResult } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartOdd, courses })
      )

      expect(evenResult.current).toHaveLength(1)
      expect(oddResult.current).toHaveLength(1)
    })

    it('should always show weekly courses regardless of week parity', () => {
      const courses = [
        createMockCourse({ id: 'weekly', frequency: 'Weekly', weekParity: 'All' }),
      ]

      const { result: evenResult } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )
      const { result: oddResult } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartOdd, courses })
      )

      expect(evenResult.current).toHaveLength(1)
      expect(oddResult.current).toHaveLength(1)
    })

    it('should correctly filter mixed frequency courses', () => {
      const courses = [
        createMockCourse({ id: 'weekly', frequency: 'Weekly', weekParity: 'All' }),
        createMockCourse({ id: 'biweekly-even', frequency: 'Biweekly', weekParity: 'Even' }),
        createMockCourse({ id: 'biweekly-odd', frequency: 'Biweekly', weekParity: 'Odd' }),
      ]

      // In even week: weekly + biweekly-even should show
      const { result: evenResult } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )
      expect(evenResult.current).toHaveLength(2)
      expect(evenResult.current.map(e => e.id)).toContain('weekly')
      expect(evenResult.current.map(e => e.id)).toContain('biweekly-even')

      // In odd week: weekly + biweekly-odd should show
      const { result: oddResult } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartOdd, courses })
      )
      expect(oddResult.current).toHaveLength(2)
      expect(oddResult.current.map(e => e.id)).toContain('weekly')
      expect(oddResult.current.map(e => e.id)).toContain('biweekly-odd')
    })
  })

  describe('Sorting', () => {
    it('should sort all events by startDateTime', () => {
      const courses = [
        createMockCourse({ id: 'c1', dayOfWeek: 'Wednesday', startTime: '16:00' }),
        createMockCourse({ id: 'c2', dayOfWeek: 'Monday', startTime: '09:00' }),
        createMockCourse({ id: 'c3', dayOfWeek: 'Tuesday', startTime: '11:00' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )

      // Verify events are sorted chronologically
      const dates = result.current.map((event) => new Date(event.startDateTime))

      // First event: Monday 09:00
      expect(dates[0].getDate()).toBe(18)
      expect(dates[0].getHours()).toBe(9)

      // Second event: Tuesday 11:00
      expect(dates[1].getDate()).toBe(19)
      expect(dates[1].getHours()).toBe(11)

      // Third event: Wednesday 16:00
      expect(dates[2].getDate()).toBe(20)
      expect(dates[2].getHours()).toBe(16)
    })
  })

  describe('Memoization', () => {
    it('should return the same reference when inputs do not change', () => {
      const courses = [createMockCourse()]

      const { result, rerender } = renderHook(
        (props) => useCalendarEvents(props),
        {
          initialProps: { weekStart: weekStartEven, courses },
        }
      )

      const firstResult = result.current

      rerender({ weekStart: weekStartEven, courses })

      expect(result.current).toBe(firstResult)
    })

    it('should return a new reference when courses change', () => {
      const courses = [createMockCourse()]

      const { result, rerender } = renderHook(
        (props) => useCalendarEvents(props),
        {
          initialProps: { weekStart: weekStartEven, courses },
        }
      )

      const firstResult = result.current

      const newCourses = [createMockCourse({ id: 'course-new' })]
      rerender({ weekStart: weekStartEven, courses: newCourses })

      expect(result.current).not.toBe(firstResult)
    })

    it('should return a new reference when weekStart changes', () => {
      const courses = [createMockCourse()]

      const { result, rerender } = renderHook(
        (props) => useCalendarEvents(props),
        {
          initialProps: { weekStart: weekStartEven, courses },
        }
      )

      const firstResult = result.current

      rerender({ weekStart: weekStartOdd, courses })

      expect(result.current).not.toBe(firstResult)
    })
  })

  describe('handles undefined/null arrays gracefully', () => {
    it('should handle undefined courses array', () => {
      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses: undefined as unknown as Course[] })
      )

      expect(result.current).toEqual([])
    })

    it('should handle course with undefined enrollments', () => {
      const courseWithoutEnrollments = createMockCourse({ enrollments: undefined })

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses: [courseWithoutEnrollments] })
      )

      expect(result.current).toHaveLength(1)
      expect(result.current[0].attendees).toEqual([])
    })

    it('should handle null enrollments in course', () => {
      const courseWithNullEnrollments = createMockCourse({ enrollments: null as unknown as Course['enrollments'] })

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses: [courseWithNullEnrollments] })
      )

      expect(result.current).toHaveLength(1)
      expect(result.current[0].attendees).toEqual([])
    })
  })

  describe('handles invalid time values gracefully', () => {
    it('should skip courses with null startTime', () => {
      const courses = [
        createMockCourse({ id: 'valid', startTime: '10:00' }),
        createMockCourse({ id: 'invalid', startTime: null as unknown as string }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )

      expect(result.current).toHaveLength(1)
    })

    it('should skip courses with undefined endTime', () => {
      const courses = [
        createMockCourse({ id: 'valid', endTime: '11:00' }),
        createMockCourse({ id: 'invalid', endTime: undefined }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )

      expect(result.current).toHaveLength(1)
    })

    it('should skip courses with invalid time format', () => {
      const courses = [
        createMockCourse({ id: 'valid', startTime: '10:00', endTime: '11:00' }),
        createMockCourse({ id: 'invalid', startTime: '10:00', endTime: 'not-a-time' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )

      expect(result.current).toHaveLength(1)
    })
  })

  describe('Timezone handling', () => {
    it('should handle course events at 09:00 local time', () => {
      const courses = [createMockCourse({ dayOfWeek: 'Monday', startTime: '09:00', endTime: '10:00' })]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart: weekStartEven, courses })
      )

      const startDate = new Date(result.current[0].startDateTime)
      const endDate = new Date(result.current[0].endDateTime)

      expect(startDate.getHours()).toBe(9)
      expect(startDate.getMinutes()).toBe(0)
      expect(endDate.getHours()).toBe(10)
      expect(endDate.getMinutes()).toBe(0)
    })
  })
})
