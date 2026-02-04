import { describe, it, expect } from 'vitest'
import { renderHook } from '@testing-library/react'
import { useCalendarEvents } from '../useCalendarEvents'
import type { CalendarLesson, Holiday } from '@/features/schedule/types'
import type { Course } from '@/features/courses/types'

describe('useCalendarEvents', () => {
  const weekStart = new Date('2024-03-18') // Monday

  const createMockLesson = (overrides: Partial<CalendarLesson> = {}): CalendarLesson => ({
    id: 'lesson-1',
    courseId: 'course-1',
    title: 'Piano Lesson',
    date: '2024-03-20',
    startTime: '14:00',
    endTime: '15:00',
    studentName: 'John Doe',
    teacherName: 'Jane Smith',
    roomName: 'Room 1',
    instrumentName: 'Piano',
    status: 'Scheduled',
    ...overrides,
  })

  const createMockCourse = (overrides: Partial<Course> = {}): Course => ({
    id: 'course-1',
    teacherId: 'teacher-1',
    teacherName: 'Jane Smith',
    courseTypeId: 1,
    courseTypeName: 'Individual Piano',
    instrumentName: 'Piano',
    roomId: 1,
    roomName: 'Room 1',
    dayOfWeek: 3, // Wednesday
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

  const createMockHoliday = (overrides: Partial<Holiday> = {}): Holiday => ({
    id: 1,
    name: 'Spring Break',
    startDate: '2024-03-18',
    endDate: '2024-03-22',
    ...overrides,
  })

  describe('Lesson transformation', () => {
    it('should transform lessons to events with correct local datetime', () => {
      const lessons = [createMockLesson({ date: '2024-03-20', startTime: '14:00', endTime: '15:00' })]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons, courses: [], holidays: [], isTrial: false })
      )

      expect(result.current).toHaveLength(1)

      // Verify the ISO string represents 14:00 local time
      const startDate = new Date(result.current[0].startDateTime)
      const endDate = new Date(result.current[0].endDateTime)

      expect(startDate.getHours()).toBe(14)
      expect(startDate.getMinutes()).toBe(0)
      expect(endDate.getHours()).toBe(15)
      expect(endDate.getMinutes()).toBe(0)
      expect(result.current[0].title).toBe('Piano Lesson')
    })

    it('should set eventType to course for non-trial lessons', () => {
      const lessons = [
        createMockLesson({ id: 'lesson-1', instrumentName: 'Individual Piano' }),
        createMockLesson({ id: 'lesson-2', instrumentName: 'Group Guitar' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons, courses: [], holidays: [], isTrial: false })
      )

      expect(result.current[0].eventType).toBe('course')
      expect(result.current[1].eventType).toBe('course')
    })

    it('should use trail eventType for lessons in trial mode', () => {
      const lessons = [createMockLesson()]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons, courses: [], holidays: [], isTrial: true })
      )

      expect(result.current[0].eventType).toBe('trail')
      expect(result.current[0].frequency).toBe('once')
    })

    it('should include student name as attendee', () => {
      const lessons = [createMockLesson({ studentName: 'Alice Johnson' })]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons, courses: [], holidays: [], isTrial: false })
      )

      expect(result.current[0].attendees).toEqual(['Alice Johnson'])
    })

    it('should handle lessons with no student name', () => {
      const lessons = [createMockLesson({ studentName: undefined })]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons, courses: [], holidays: [], isTrial: false })
      )

      expect(result.current[0].attendees).toEqual([])
    })
  })

  describe('Course transformation', () => {
    it('should transform courses to events with calculated date and local time', () => {
      const courses = [createMockCourse({ dayOfWeek: 3, startTime: '10:00', endTime: '11:00' })] // Wednesday

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses, holidays: [], isTrial: false })
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
        createMockCourse({ id: 'course-1', dayOfWeek: 1, startTime: '09:00' }), // Monday
        createMockCourse({ id: 'course-2', dayOfWeek: 5, startTime: '11:00' }), // Friday
        createMockCourse({ id: 'course-3', dayOfWeek: 0, startTime: '13:00' }), // Sunday
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses, holidays: [], isTrial: false })
      )

      expect(result.current[0].startDateTime).toContain('2024-03-18') // Monday
      expect(result.current[1].startDateTime).toContain('2024-03-22') // Friday
      expect(result.current[2].startDateTime).toContain('2024-03-24') // Sunday
    })

    it('should not include courses in trial mode', () => {
      const courses = [createMockCourse()]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses, holidays: [], isTrial: true })
      )

      expect(result.current).toHaveLength(0)
    })

    it('should skip future courses', () => {
      const courses = [
        createMockCourse({ id: 'past', startDate: '2020-01-01' }),
        createMockCourse({ id: 'future', startDate: '2099-12-31' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses, holidays: [], isTrial: false })
      )

      expect(result.current).toHaveLength(1)
    })

    it('should determine eventType based on isWorkshop flag', () => {
      const courses = [
        createMockCourse({ id: 'c1', courseTypeName: 'Individual Piano', isWorkshop: false }),
        createMockCourse({ id: 'c2', courseTypeName: 'Group Guitar', isWorkshop: false }),
        createMockCourse({ id: 'c3', isWorkshop: true, courseTypeName: 'Drums Workshop' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses, holidays: [], isTrial: false })
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
        useCalendarEvents({ weekStart, lessons: [], courses, holidays: [], isTrial: false })
      )

      expect(result.current[0].attendees).toEqual(['Bob Smith', 'Carol White'])
    })

    it('should include room as string', () => {
      const courses = [createMockCourse({ roomId: 42 })]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses, holidays: [], isTrial: false })
      )

      expect(result.current[0].room).toBe('42')
    })

    it('should preserve frequency from course', () => {
      const courses = [
        createMockCourse({ id: 'c1', frequency: 'Weekly' }),
        createMockCourse({ id: 'c2', frequency: 'Biweekly' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses, holidays: [], isTrial: false })
      )

      expect(result.current[0].frequency).toBe('weekly')
      expect(result.current[1].frequency).toBe('bi-weekly')
    })
  })

  describe('Holiday transformation', () => {
    it('should transform holidays to all-day events', () => {
      const holidays = [createMockHoliday({ startDate: '2024-03-20', endDate: '2024-03-20', name: 'Teacher Day' })]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses: [], holidays, isTrial: false })
      )

      expect(result.current).toHaveLength(1)
      expect(result.current[0].title).toBe('Teacher Day')
      expect(result.current[0].eventType).toBe('holiday')
      expect(result.current[0].frequency).toBe('once')
    })

    it('should create events for each day of a multi-day holiday within the week', () => {
      const holidays = [createMockHoliday({ startDate: '2024-03-18', endDate: '2024-03-20' })]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses: [], holidays, isTrial: false })
      )

      // Should create 3 events: Monday, Tuesday, Wednesday
      expect(result.current).toHaveLength(3)
      expect(result.current.every((e) => e.eventType === 'holiday')).toBe(true)
    })

    it('should only include holiday days that overlap with the displayed week', () => {
      // Holiday spans beyond the week
      const holidays = [createMockHoliday({ startDate: '2024-03-15', endDate: '2024-03-30' })]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses: [], holidays, isTrial: false })
      )

      // Should only create events for Monday-Sunday of the week (7 days)
      expect(result.current).toHaveLength(7)
    })

    it('should handle holiday that starts before and ends within the week', () => {
      const holidays = [createMockHoliday({ startDate: '2024-03-10', endDate: '2024-03-19' })]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses: [], holidays, isTrial: false })
      )

      // Should include Monday and Tuesday only
      expect(result.current).toHaveLength(2)
    })
  })

  describe('Sorting', () => {
    it('should sort all events by startDateTime', () => {
      const lessons = [
        createMockLesson({ id: 'l1', date: '2024-03-20', startTime: '16:00' }),
        createMockLesson({ id: 'l2', date: '2024-03-18', startTime: '09:00' }),
      ]
      const courses = [createMockCourse({ dayOfWeek: 2, startTime: '11:00' })] // Tuesday

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons, courses, holidays: [], isTrial: false })
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

      // Verify chronological order
      expect(dates[0].getTime()).toBeLessThan(dates[1].getTime())
      expect(dates[1].getTime()).toBeLessThan(dates[2].getTime())
    })
  })

  describe('Combined data', () => {
    it('should combine lessons, courses, and holidays into single events array', () => {
      const lessons = [createMockLesson()]
      const courses = [createMockCourse()]
      const holidays = [createMockHoliday({ startDate: '2024-03-18', endDate: '2024-03-18' })]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons, courses, holidays, isTrial: false })
      )

      // 1 lesson + 1 course + 1 holiday day
      expect(result.current).toHaveLength(3)
      expect(result.current.some((e) => e.eventType === 'holiday')).toBe(true)
      expect(result.current.some((e) => e.eventType === 'course')).toBe(true)
    })
  })

  describe('Memoization', () => {
    it('should return the same reference when inputs do not change', () => {
      const lessons = [createMockLesson()]
      const courses = [createMockCourse()]
      const holidays: Holiday[] = []

      const { result, rerender } = renderHook(
        (props) => useCalendarEvents(props),
        {
          initialProps: { weekStart, lessons, courses, holidays, isTrial: false },
        }
      )

      const firstResult = result.current

      rerender({ weekStart, lessons, courses, holidays, isTrial: false })

      expect(result.current).toBe(firstResult)
    })

    it('should return a new reference when lessons change', () => {
      const lessons = [createMockLesson()]
      const courses = [createMockCourse()]
      const holidays: Holiday[] = []

      const { result, rerender } = renderHook(
        (props) => useCalendarEvents(props),
        {
          initialProps: { weekStart, lessons, courses, holidays, isTrial: false },
        }
      )

      const firstResult = result.current

      const newLessons = [createMockLesson({ id: 'lesson-new' })]
      rerender({ weekStart, lessons: newLessons, courses, holidays, isTrial: false })

      expect(result.current).not.toBe(firstResult)
    })
  })

  describe('Timezone handling', () => {
    it('should display events at their local time regardless of browser timezone', () => {
      // A 10:00 lesson should always display at 10:00 local time
      const lessons = [createMockLesson({ date: '2024-03-20', startTime: '10:00', endTime: '11:00' })]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons, courses: [], holidays: [], isTrial: false })
      )

      const startDate = new Date(result.current[0].startDateTime)
      const endDate = new Date(result.current[0].endDateTime)

      // Verify times match the input times in local timezone
      expect(startDate.getHours()).toBe(10)
      expect(startDate.getMinutes()).toBe(0)
      expect(endDate.getHours()).toBe(11)
      expect(endDate.getMinutes()).toBe(0)
    })

    it('should handle events near midnight boundaries correctly', () => {
      // Late evening event (23:00-23:59)
      const lateLessons = [createMockLesson({ id: 'late', date: '2024-03-20', startTime: '23:00', endTime: '23:59' })]
      // Early morning event (00:00-01:00)
      const earlyLessons = [createMockLesson({ id: 'early', date: '2024-03-20', startTime: '00:00', endTime: '01:00' })]

      const { result: lateResult } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: lateLessons, courses: [], holidays: [], isTrial: false })
      )

      const { result: earlyResult } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: earlyLessons, courses: [], holidays: [], isTrial: false })
      )

      const lateStart = new Date(lateResult.current[0].startDateTime)
      const lateEnd = new Date(lateResult.current[0].endDateTime)
      const earlyStart = new Date(earlyResult.current[0].startDateTime)
      const earlyEnd = new Date(earlyResult.current[0].endDateTime)

      // Verify late event
      expect(lateStart.getHours()).toBe(23)
      expect(lateStart.getMinutes()).toBe(0)
      expect(lateEnd.getHours()).toBe(23)
      expect(lateEnd.getMinutes()).toBe(59)

      // Verify early event
      expect(earlyStart.getHours()).toBe(0)
      expect(earlyStart.getMinutes()).toBe(0)
      expect(earlyEnd.getHours()).toBe(1)
      expect(earlyEnd.getMinutes()).toBe(0)

      // Ensure they're on the same date
      expect(lateStart.getDate()).toBe(20)
      expect(earlyStart.getDate()).toBe(20)
    })

    it('should handle course events at 09:00 local time', () => {
      // Morning course should display at 09:00 local time
      const courses = [createMockCourse({ dayOfWeek: 1, startTime: '09:00', endTime: '10:00' })] // Monday

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses, holidays: [], isTrial: false })
      )

      const startDate = new Date(result.current[0].startDateTime)
      const endDate = new Date(result.current[0].endDateTime)

      expect(startDate.getHours()).toBe(9)
      expect(startDate.getMinutes()).toBe(0)
      expect(endDate.getHours()).toBe(10)
      expect(endDate.getMinutes()).toBe(0)
    })

    it('should handle holidays as all-day events in local timezone', () => {
      const holidays = [createMockHoliday({ startDate: '2024-03-20', endDate: '2024-03-20', name: 'Test Holiday' })]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses: [], holidays, isTrial: false })
      )

      const startDate = new Date(result.current[0].startDateTime)
      const endDate = new Date(result.current[0].endDateTime)

      // Holiday should span the entire day in local time
      expect(startDate.getHours()).toBe(0)
      expect(startDate.getMinutes()).toBe(0)
      expect(startDate.getSeconds()).toBe(0)

      expect(endDate.getHours()).toBe(23)
      expect(endDate.getMinutes()).toBe(59)
      expect(endDate.getSeconds()).toBe(59)

      // Both should be on the same date
      expect(startDate.getDate()).toBe(20)
      expect(endDate.getDate()).toBe(20)
    })

    it('should maintain date consistency across different time inputs', () => {
      const lessons = [
        createMockLesson({ id: 'l1', date: '2024-03-20', startTime: '08:30' }),
        createMockLesson({ id: 'l2', date: '2024-03-20', startTime: '14:45' }),
        createMockLesson({ id: 'l3', date: '2024-03-20', startTime: '20:15' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons, courses: [], holidays: [], isTrial: false })
      )

      // All events should be on the same date
      result.current.forEach((event) => {
        const eventDate = new Date(event.startDateTime)
        expect(eventDate.getDate()).toBe(20)
        expect(eventDate.getMonth()).toBe(2) // March (0-indexed)
        expect(eventDate.getFullYear()).toBe(2024)
      })

      // Verify specific times
      expect(new Date(result.current[0].startDateTime).getHours()).toBe(8)
      expect(new Date(result.current[0].startDateTime).getMinutes()).toBe(30)

      expect(new Date(result.current[1].startDateTime).getHours()).toBe(14)
      expect(new Date(result.current[1].startDateTime).getMinutes()).toBe(45)

      expect(new Date(result.current[2].startDateTime).getHours()).toBe(20)
      expect(new Date(result.current[2].startDateTime).getMinutes()).toBe(15)
    })
  })

  describe('handles undefined arrays gracefully', () => {
    it('should handle undefined lessons array', () => {
      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: undefined as unknown as CalendarLesson[], courses: [], holidays: [], isTrial: false })
      )

      expect(result.current).toEqual([])
    })

    it('should handle undefined courses array', () => {
      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses: undefined as unknown as Course[], holidays: [], isTrial: false })
      )

      expect(result.current).toEqual([])
    })

    it('should handle undefined holidays array', () => {
      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses: [], holidays: undefined as unknown as Holiday[], isTrial: false })
      )

      expect(result.current).toEqual([])
    })

    it('should handle all arrays being undefined', () => {
      const { result } = renderHook(() =>
        useCalendarEvents({
          weekStart,
          lessons: undefined as unknown as CalendarLesson[],
          courses: undefined as unknown as Course[],
          holidays: undefined as unknown as Holiday[],
          isTrial: false,
        })
      )

      expect(result.current).toEqual([])
    })

    it('should handle course with undefined enrollments', () => {
      const courseWithoutEnrollments = createMockCourse({ enrollments: undefined })

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses: [courseWithoutEnrollments], holidays: [], isTrial: false })
      )

      expect(result.current).toHaveLength(1)
      expect(result.current[0].attendees).toEqual([])
    })

    it('should handle null enrollments in course', () => {
      const courseWithNullEnrollments = createMockCourse({ enrollments: null as unknown as Course['enrollments'] })

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses: [courseWithNullEnrollments], holidays: [], isTrial: false })
      )

      expect(result.current).toHaveLength(1)
      expect(result.current[0].attendees).toEqual([])
    })
  })

  describe('handles invalid time/date values gracefully', () => {
    it('should skip lessons with null startTime', () => {
      const lessons = [
        createMockLesson({ id: 'valid', startTime: '10:00' }),
        createMockLesson({ id: 'invalid', startTime: null as unknown as string }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons, courses: [], holidays: [], isTrial: false })
      )

      // Should only include the valid lesson
      expect(result.current).toHaveLength(1)
      expect(result.current[0].title).toBe('Piano Lesson')
    })

    it('should skip lessons with undefined startTime', () => {
      const lessons = [
        createMockLesson({ id: 'valid', startTime: '10:00' }),
        createMockLesson({ id: 'invalid', startTime: undefined }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons, courses: [], holidays: [], isTrial: false })
      )

      expect(result.current).toHaveLength(1)
    })

    it('should skip lessons with empty string startTime', () => {
      const lessons = [
        createMockLesson({ id: 'valid', startTime: '10:00' }),
        createMockLesson({ id: 'invalid', startTime: '' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons, courses: [], holidays: [], isTrial: false })
      )

      expect(result.current).toHaveLength(1)
    })

    it('should skip lessons with invalid time format', () => {
      const lessons = [
        createMockLesson({ id: 'valid', startTime: '10:00' }),
        createMockLesson({ id: 'invalid', startTime: 'invalid' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons, courses: [], holidays: [], isTrial: false })
      )

      expect(result.current).toHaveLength(1)
    })

    it('should skip lessons with null endTime', () => {
      const lessons = [
        createMockLesson({ id: 'valid', endTime: '11:00' }),
        createMockLesson({ id: 'invalid', endTime: null as unknown as string }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons, courses: [], holidays: [], isTrial: false })
      )

      expect(result.current).toHaveLength(1)
    })

    it('should skip lessons with invalid date', () => {
      const lessons = [
        createMockLesson({ id: 'valid', date: '2024-03-20' }),
        createMockLesson({ id: 'invalid', date: 'invalid-date' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons, courses: [], holidays: [], isTrial: false })
      )

      expect(result.current).toHaveLength(1)
    })

    it('should skip courses with null startTime', () => {
      const courses = [
        createMockCourse({ id: 'valid', startTime: '10:00' }),
        createMockCourse({ id: 'invalid', startTime: null as unknown as string }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses, holidays: [], isTrial: false })
      )

      expect(result.current).toHaveLength(1)
    })

    it('should skip courses with undefined endTime', () => {
      const courses = [
        createMockCourse({ id: 'valid', endTime: '11:00' }),
        createMockCourse({ id: 'invalid', endTime: undefined }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses, holidays: [], isTrial: false })
      )

      expect(result.current).toHaveLength(1)
    })

    it('should skip courses with invalid time format', () => {
      const courses = [
        createMockCourse({ id: 'valid', startTime: '10:00', endTime: '11:00' }),
        createMockCourse({ id: 'invalid', startTime: '10:00', endTime: 'not-a-time' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons: [], courses, holidays: [], isTrial: false })
      )

      expect(result.current).toHaveLength(1)
    })

    it('should handle mixed valid and invalid data gracefully', () => {
      const lessons = [
        createMockLesson({ id: 'l1', startTime: '10:00', endTime: '11:00' }),
        createMockLesson({ id: 'l2-invalid', startTime: null as unknown as string }),
        createMockLesson({ id: 'l3', startTime: '14:00', endTime: '15:00' }),
      ]
      const courses = [
        createMockCourse({ id: 'c1', startTime: '09:00', endTime: '10:00' }),
        createMockCourse({ id: 'c2-invalid', startTime: 'bad' }),
      ]

      const { result } = renderHook(() =>
        useCalendarEvents({ weekStart, lessons, courses, holidays: [], isTrial: false })
      )

      // Should include 2 valid lessons + 1 valid course
      expect(result.current).toHaveLength(3)
    })
  })
})
