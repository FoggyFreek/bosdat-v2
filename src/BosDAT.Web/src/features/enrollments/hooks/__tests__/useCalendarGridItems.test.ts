import { describe, it, expect } from 'vitest'
import { renderHook } from '@testing-library/react'
import { useCalendarGridItems } from '../useCalendarGridItems'
import type { CalendarLesson } from '@/features/schedule/types'
import type { Course } from '@/features/courses/types'

describe('useCalendarGridItems', () => {
  const mockDate = '2024-03-20' // Wednesday
  const weekStart = new Date('2024-03-18') // Monday of that week

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
        useCalendarGridItems({ date: mockDate, weekStart, lessons, courses, isTrial: true })
      )

      expect(result.current).toHaveLength(2)
      expect(result.current[0].type).toBe('lesson')
      expect(result.current[1].type).toBe('lesson')
      expect(result.current.every(item => item.id.startsWith('lesson-'))).toBe(true)
    })

    it('should use "Trail" as course type for all lessons in trail mode', () => {
      const lessons = [createMockLesson()]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, weekStart, lessons, courses: [], isTrial: true })
      )

      expect(result.current[0].courseType).toBe('Trail')
    })

    it('should preserve lesson dates in trail mode', () => {
      const lessons = [
        createMockLesson({ id: 'lesson-1', date: '2024-03-18', startTime: '10:00' }),
        createMockLesson({ id: 'lesson-2', date: '2024-03-20', startTime: '14:00' }),
      ]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, weekStart, lessons, courses: [], isTrial: true })
      )

      expect(result.current[0].date).toBe('2024-03-18')
      expect(result.current[1].date).toBe('2024-03-20')
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
        useCalendarGridItems({ date: mockDate, weekStart, lessons, courses, isTrial: false })
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
        useCalendarGridItems({ date: mockDate, weekStart, lessons: [], courses, isTrial: false })
      )

      expect(result.current[0].isFuture).toBe(true)
    })

    it('should not mark courses starting in the past as isFuture', () => {
      const pastDate = '2020-01-01'
      const courses = [
        createMockCourse({ id: 'course-1', startDate: pastDate }),
      ]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, weekStart, lessons: [], courses, isTrial: false })
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
        useCalendarGridItems({ date: mockDate, weekStart, lessons: [], courses, isTrial: false })
      )

      expect(result.current[0].courseType).toBe('Individual')
      expect(result.current[1].courseType).toBe('Group')
      expect(result.current[2].courseType).toBe('Workshop')
    })

    it('should calculate correct dates for courses based on dayOfWeek', () => {
      const courses = [
        createMockCourse({ id: 'course-1', dayOfWeek: 1 }), // Monday
        createMockCourse({ id: 'course-2', dayOfWeek: 3 }), // Wednesday
        createMockCourse({ id: 'course-3', dayOfWeek: 5 }), // Friday
      ]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, weekStart, lessons: [], courses, isTrial: false })
      )

      expect(result.current[0].date).toBe('2024-03-18') // Monday
      expect(result.current[1].date).toBe('2024-03-20') // Wednesday
      expect(result.current[2].date).toBe('2024-03-22') // Friday
    })

    it('should handle Sunday courses (dayOfWeek = 0)', () => {
      const courses = [
        createMockCourse({ id: 'course-1', dayOfWeek: 0 }), // Sunday
      ]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, weekStart, lessons: [], courses, isTrial: false })
      )

      // Sunday is weekStart + 6 days (end of week)
      expect(result.current[0].date).toBe('2024-03-24')
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
        useCalendarGridItems({ date: mockDate, weekStart, lessons, courses: [], isTrial: true })
      )

      expect(result.current[0].startTime).toBe('10:00')
      expect(result.current[1].startTime).toBe('14:00')
      expect(result.current[2].startTime).toBe('16:00')
    })

    it('should sort mixed lessons and courses by start time', () => {
      const lessons = [createMockLesson({ startTime: '16:00' })]
      const courses = [createMockCourse({ startTime: '10:00' })]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, weekStart, lessons, courses, isTrial: false })
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
        useCalendarGridItems({ date: mockDate, weekStart, lessons, courses: [], isTrial: true })
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
        useCalendarGridItems({ date: mockDate, weekStart, lessons: [], courses, isTrial: false })
      )

      expect(result.current[0].studentNames).toEqual(['Bob Smith', 'Carol White'])
    })

    it('should handle lessons with no student name', () => {
      const lessons = [createMockLesson({ studentName: undefined })]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, weekStart, lessons, courses: [], isTrial: true })
      )

      expect(result.current[0].studentNames).toEqual([])
    })
  })

  describe('Frequency mapping', () => {
    it('should map "Weekly" course frequency', () => {
      const courses = [createMockCourse({ frequency: 'Weekly' })]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, weekStart, lessons: [], courses, isTrial: false })
      )

      expect(result.current[0].frequency).toBe('Weekly')
    })

    it('should map "Biweekly" course frequency', () => {
      const courses = [createMockCourse({ frequency: 'Biweekly' })]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, weekStart, lessons: [], courses, isTrial: false })
      )

      expect(result.current[0].frequency).toBe('Biweekly')
    })

    it('should use "Trail" frequency for lessons in trail mode', () => {
      const lessons = [createMockLesson()]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, weekStart, lessons, courses: [], isTrial: true })
      )

      expect(result.current[0].frequency).toBe('Trail')
    })

    it('should not set frequency for lessons in course mode', () => {
      const lessons = [createMockLesson()]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, weekStart, lessons, courses: [], isTrial: false })
      )

      expect(result.current[0].frequency).toBeUndefined()
    })
  })

  describe('Memoization', () => {
    it('should return the same reference when inputs do not change', () => {
      const lessons = [createMockLesson()]
      const courses = [createMockCourse()]

      const { result, rerender } = renderHook(
        ({ date, weekStart, lessons, courses, isTrial }) =>
          useCalendarGridItems({ date, weekStart, lessons, courses, isTrial }),
        {
          initialProps: { date: mockDate, weekStart, lessons, courses, isTrial: false },
        }
      )

      const firstResult = result.current

      rerender({ date: mockDate, weekStart, lessons, courses, isTrial: false })

      expect(result.current).toBe(firstResult)
    })

    it('should return a new reference when inputs change', () => {
      const lessons = [createMockLesson()]
      const courses = [createMockCourse()]

      const { result, rerender } = renderHook(
        ({ date, weekStart, lessons, courses, isTrial }) =>
          useCalendarGridItems({ date, weekStart, lessons, courses, isTrial }),
        {
          initialProps: { date: mockDate, weekStart, lessons, courses, isTrial: false },
        }
      )

      const firstResult = result.current

      const newLessons = [createMockLesson({ id: 'lesson-new' })]
      rerender({ date: mockDate, weekStart, lessons: newLessons, courses, isTrial: false })

      expect(result.current).not.toBe(firstResult)
    })

    it('should return a new reference when weekStart changes', () => {
      const lessons = [createMockLesson()]
      const courses = [createMockCourse()]

      const { result, rerender } = renderHook(
        ({ date, weekStart, lessons, courses, isTrial }) =>
          useCalendarGridItems({ date, weekStart, lessons, courses, isTrial }),
        {
          initialProps: { date: mockDate, weekStart, lessons, courses, isTrial: false },
        }
      )

      const firstResult = result.current

      const newWeekStart = new Date('2024-03-25') // Next week
      rerender({ date: mockDate, weekStart: newWeekStart, lessons, courses, isTrial: false })

      expect(result.current).not.toBe(firstResult)
    })
  })

  describe('Events across multiple dates in a week', () => {
    it('should handle lessons and courses on different days of the week', () => {
      const lessons = [
        createMockLesson({ id: 'lesson-1', date: '2024-03-18', startTime: '09:00' }), // Monday
        createMockLesson({ id: 'lesson-2', date: '2024-03-20', startTime: '14:00' }), // Wednesday
        createMockLesson({ id: 'lesson-3', date: '2024-03-22', startTime: '16:00' }), // Friday
      ]
      const courses = [
        createMockCourse({ id: 'course-1', dayOfWeek: 2, startTime: '10:00' }), // Tuesday
        createMockCourse({ id: 'course-2', dayOfWeek: 4, startTime: '15:00' }), // Thursday
      ]

      const { result } = renderHook(() =>
        useCalendarGridItems({ date: mockDate, weekStart, lessons, courses, isTrial: false })
      )

      expect(result.current).toHaveLength(5)

      // Verify lessons preserved their dates
      const mondayLesson = result.current.find(item => item.id === 'lesson-1')
      const wednesdayLesson = result.current.find(item => item.id === 'lesson-2')
      const fridayLesson = result.current.find(item => item.id === 'lesson-3')

      expect(mondayLesson?.date).toBe('2024-03-18')
      expect(wednesdayLesson?.date).toBe('2024-03-20')
      expect(fridayLesson?.date).toBe('2024-03-22')

      // Verify courses calculated their dates correctly
      const tuesdayCourse = result.current.find(item => item.id === 'course-1')
      const thursdayCourse = result.current.find(item => item.id === 'course-2')

      expect(tuesdayCourse?.date).toBe('2024-03-19')
      expect(thursdayCourse?.date).toBe('2024-03-21')

      // Verify all items have dates
      expect(result.current.every(item => item.date !== undefined)).toBe(true)
    })

    it('should maintain correct dates when weekStart changes', () => {
      const courses = [
        createMockCourse({ id: 'course-1', dayOfWeek: 1 }), // Monday
        createMockCourse({ id: 'course-2', dayOfWeek: 5 }), // Friday
      ]

      const { result, rerender } = renderHook(
        ({ weekStart }) =>
          useCalendarGridItems({ date: mockDate, weekStart, lessons: [], courses, isTrial: false }),
        {
          initialProps: { weekStart },
        }
      )

      // First week
      expect(result.current[0].date).toBe('2024-03-18') // Monday
      expect(result.current[1].date).toBe('2024-03-22') // Friday

      // Move to next week
      const nextWeekStart = new Date('2024-03-25')
      rerender({ weekStart: nextWeekStart })

      expect(result.current[0].date).toBe('2024-03-25') // Monday of next week
      expect(result.current[1].date).toBe('2024-03-29') // Friday of next week
    })
  })
})
