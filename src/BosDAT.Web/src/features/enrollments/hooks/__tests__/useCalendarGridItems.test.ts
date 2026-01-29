import { describe, it, expect } from 'vitest'
import { renderHook } from '@testing-library/react'
import { useCalendarGridItems } from '../useCalendarGridItems'
import type { CalendarLesson } from '@/features/schedule/types'
import type { Course } from '@/features/courses/types'

describe('useCalendarGridItems', () => {
  const mockDate = '2024-03-20' // Wednesday

  const createMockLesson = (overrides: Partial<CalendarLesson> = {}): CalendarLesson => ({
    id: 'lesson-1',
    title: 'Piano Lesson',
    date: mockDate,
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

  describe('Trail mode (isTrial = true)', () => {
    it('should transform lessons only, not courses', () => {
      const lessons = [
        createMockLesson({ id: 'lesson-1', startTime: '14:00', endTime: '15:00' }),
        createMockLesson({ id: 'lesson-2', startTime: '16:00', endTime: '17:00' }),
      ]
      const courses = [
        createMockCourse({ id: 'course-1', startTime: '10:00', endTime: '11:00' }),
      ]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, lessons, courses, isTrial: true })
      )

      expect(result.current).toHaveLength(2)
      expect(result.current[0].type).toBe('lesson')
      expect(result.current[1].type).toBe('lesson')
      expect(result.current.every(item => item.id.startsWith('lesson-'))).toBe(true)
    })

    it('should use "Trail" as course type for all lessons in trail mode', () => {
      const lessons = [createMockLesson()]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, lessons, courses: [], isTrial: true })
      )

      expect(result.current[0].courseType).toBe('Trail')
    })
  })

  describe('Course mode (isTrial = false)', () => {
    it('should transform both lessons and courses', () => {
      const lessons = [
        createMockLesson({ id: 'lesson-1', startTime: '14:00', endTime: '15:00' }),
      ]
      const courses = [
        createMockCourse({ id: 'course-1', startTime: '10:00', endTime: '11:00' }),
      ]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, lessons, courses, isTrial: false })
      )

      expect(result.current).toHaveLength(2)
      expect(result.current.some(item => item.type === 'lesson')).toBe(true)
      expect(result.current.some(item => item.type === 'course')).toBe(true)
    })

    it('should mark courses starting in the future as isFuture', () => {
      const futureDate = '2099-12-31'
      const courses = [
        createMockCourse({ id: 'course-1', startDate: futureDate }),
      ]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, lessons: [], courses, isTrial: false })
      )

      expect(result.current[0].isFuture).toBe(true)
    })

    it('should not mark courses starting in the past as isFuture', () => {
      const pastDate = '2020-01-01'
      const courses = [
        createMockCourse({ id: 'course-1', startDate: pastDate }),
      ]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, lessons: [], courses, isTrial: false })
      )

      expect(result.current[0].isFuture).toBe(false)
    })

    it('should determine courseType from course name', () => {
      const courses = [
        createMockCourse({ courseTypeName: 'Individual Piano' }),
        createMockCourse({ id: 'course-2', courseTypeName: 'Group Guitar' }),
        createMockCourse({ id: 'course-3', isWorkshop: true, courseTypeName: 'Workshop Drums' }),
      ]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, lessons: [], courses, isTrial: false })
      )

      expect(result.current[0].courseType).toBe('Individual')
      expect(result.current[1].courseType).toBe('Group')
      expect(result.current[2].courseType).toBe('Workshop')
    })
  })

  describe('Sorting', () => {
    it('should sort items by start time', () => {
      const lessons = [
        createMockLesson({ id: 'lesson-1', startTime: '16:00' }),
        createMockLesson({ id: 'lesson-2', startTime: '10:00' }),
        createMockLesson({ id: 'lesson-3', startTime: '14:00' }),
      ]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, lessons, courses: [], isTrial: true })
      )

      expect(result.current[0].startTime).toBe('10:00')
      expect(result.current[1].startTime).toBe('14:00')
      expect(result.current[2].startTime).toBe('16:00')
    })

    it('should sort mixed lessons and courses by start time', () => {
      const lessons = [createMockLesson({ startTime: '16:00' })]
      const courses = [createMockCourse({ startTime: '10:00' })]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, lessons, courses, isTrial: false })
      )

      expect(result.current[0].startTime).toBe('10:00')
      expect(result.current[0].type).toBe('course')
      expect(result.current[1].startTime).toBe('16:00')
      expect(result.current[1].type).toBe('lesson')
    })
  })

  describe('Student names transformation', () => {
    it('should use studentName from lesson for lessons', () => {
      const lessons = [createMockLesson({ studentName: 'Alice Johnson' })]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, lessons, courses: [], isTrial: true })
      )

      expect(result.current[0].studentNames).toEqual(['Alice Johnson'])
    })

    it('should extract student names from enrollments for courses', () => {
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
        useCalendarGridItems({ date: mockDate, lessons: [], courses, isTrial: false })
      )

      expect(result.current[0].studentNames).toEqual(['Bob Smith', 'Carol White'])
    })

    it('should handle lessons with no student name', () => {
      const lessons = [createMockLesson({ studentName: undefined })]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, lessons, courses: [], isTrial: true })
      )

      expect(result.current[0].studentNames).toEqual([])
    })
  })

  describe('Frequency mapping', () => {
    it('should map "Weekly" course frequency', () => {
      const courses = [createMockCourse({ frequency: 'Weekly' })]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, lessons: [], courses, isTrial: false })
      )

      expect(result.current[0].frequency).toBe('Weekly')
    })

    it('should map "Biweekly" course frequency', () => {
      const courses = [createMockCourse({ frequency: 'Biweekly' })]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, lessons: [], courses, isTrial: false })
      )

      expect(result.current[0].frequency).toBe('Biweekly')
    })

    it('should use "Trail" frequency for lessons in trail mode', () => {
      const lessons = [createMockLesson()]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, lessons, courses: [], isTrial: true })
      )

      expect(result.current[0].frequency).toBe('Trail')
    })

    it('should not set frequency for lessons in course mode', () => {
      const lessons = [createMockLesson()]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, lessons, courses: [], isTrial: false })
      )

      expect(result.current[0].frequency).toBeUndefined()
    })
  })

  describe('Memoization', () => {
    it('should return the same reference when inputs do not change', () => {
      const lessons = [createMockLesson()]
      const courses = [createMockCourse()]

      const { result, rerender } = renderHook(
        ({ date, lessons, courses, isTrial }) =>
          useCalendarGridItems({ date, lessons, courses, isTrial }),
        {
          initialProps: { date: mockDate, lessons, courses, isTrial: false },
        }
      )

      const firstResult = result.current

      rerender({ date: mockDate, lessons, courses, isTrial: false })

      expect(result.current).toBe(firstResult)
    })

    it('should return a new reference when inputs change', () => {
      const lessons = [createMockLesson()]
      const courses = [createMockCourse()]

      const { result, rerender } = renderHook(
        ({ date, lessons, courses, isTrial }) =>
          useCalendarGridItems({ date, lessons, courses, isTrial }),
        {
          initialProps: { date: mockDate, lessons, courses, isTrial: false },
        }
      )

      const firstResult = result.current

      const newLessons = [createMockLesson({ id: 'lesson-new' })]
      rerender({ date: mockDate, lessons: newLessons, courses, isTrial: false })

      expect(result.current).not.toBe(firstResult)
    })
  })
})
