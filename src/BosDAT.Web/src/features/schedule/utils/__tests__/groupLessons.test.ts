import { describe, it, expect } from 'vitest'
import {
  groupLessonsByCourseAndDate,
  deriveGroupStatus,
  isGroupLesson,
} from '../groupLessons'
import type { CalendarLesson } from '../../types'

const createLesson = (overrides: Partial<CalendarLesson> = {}): CalendarLesson => ({
  id: crypto.randomUUID(),
  courseId: 'course-1',
  studentId: 'student-1',
  title: 'Piano - Student1',
  date: '2024-03-07',
  startTime: '10:00',
  frequency: 'Weekly',
  endTime: '10:30',
  studentName: 'Student1 Test',
  teacherName: 'Teacher Test',
  roomName: 'Room 1',
  instrumentName: 'Piano',
  status: 'Scheduled',
  ...overrides,
})

describe('groupLessonsByCourseAndDate', () => {
  it('groups lessons with same courseId and date', () => {
    const lessons: CalendarLesson[] = [
      createLesson({ courseId: 'course-1', date: '2024-03-07', studentId: 'student-1', studentName: 'Alice' }),
      createLesson({ courseId: 'course-1', date: '2024-03-07', studentId: 'student-2', studentName: 'Bob' }),
      createLesson({ courseId: 'course-1', date: '2024-03-07', studentId: 'student-3', studentName: 'Charlie' }),
    ]

    const grouped = groupLessonsByCourseAndDate(lessons)

    expect(grouped).toHaveLength(1)
    expect(grouped[0].lessons).toHaveLength(3)
    expect(grouped[0].studentNames).toEqual(['Alice', 'Bob', 'Charlie'])
    expect(grouped[0].courseId).toBe('course-1')
    expect(grouped[0].date).toBe('2024-03-07')
  })

  it('separates lessons from different courses', () => {
    const lessons: CalendarLesson[] = [
      createLesson({ courseId: 'course-1', date: '2024-03-07', studentName: 'Alice' }),
      createLesson({ courseId: 'course-2', date: '2024-03-07', studentName: 'Bob' }),
    ]

    const grouped = groupLessonsByCourseAndDate(lessons)

    expect(grouped).toHaveLength(2)
    expect(grouped.find((g) => g.courseId === 'course-1')?.studentNames).toEqual(['Alice'])
    expect(grouped.find((g) => g.courseId === 'course-2')?.studentNames).toEqual(['Bob'])
  })

  it('separates lessons from different dates', () => {
    const lessons: CalendarLesson[] = [
      createLesson({ courseId: 'course-1', date: '2024-03-07', studentName: 'Alice' }),
      createLesson({ courseId: 'course-1', date: '2024-03-14', studentName: 'Alice' }),
    ]

    const grouped = groupLessonsByCourseAndDate(lessons)

    expect(grouped).toHaveLength(2)
    expect(grouped.find((g) => g.date === '2024-03-07')).toBeDefined()
    expect(grouped.find((g) => g.date === '2024-03-14')).toBeDefined()
  })

  it('preserves lesson metadata from first lesson in group', () => {
    const lessons: CalendarLesson[] = [
      createLesson({
        courseId: 'course-1',
        date: '2024-03-07',
        startTime: '14:00',
        endTime: '14:45',
        teacherName: 'John Teacher',
        roomName: 'Studio A',
        instrumentName: 'Guitar',
      }),
      createLesson({
        courseId: 'course-1',
        date: '2024-03-07',
      }),
    ]

    const grouped = groupLessonsByCourseAndDate(lessons)

    expect(grouped[0].startTime).toBe('14:00')
    expect(grouped[0].endTime).toBe('14:45')
    expect(grouped[0].teacherName).toBe('John Teacher')
    expect(grouped[0].roomName).toBe('Studio A')
    expect(grouped[0].instrumentName).toBe('Guitar')
  })

  it('filters out null student names', () => {
    const lessons: CalendarLesson[] = [
      createLesson({ studentName: 'Alice' }),
      createLesson({ studentName: undefined, studentId: undefined }),
      createLesson({ studentName: 'Charlie' }),
    ]

    const grouped = groupLessonsByCourseAndDate(lessons)

    expect(grouped[0].studentNames).toEqual(['Alice', 'Charlie'])
  })

  it('returns empty array for empty input', () => {
    const grouped = groupLessonsByCourseAndDate([])
    expect(grouped).toEqual([])
  })

  it('handles single lesson as its own group', () => {
    const lessons: CalendarLesson[] = [createLesson({ studentName: 'Solo Student' })]

    const grouped = groupLessonsByCourseAndDate(lessons)

    expect(grouped).toHaveLength(1)
    expect(grouped[0].lessons).toHaveLength(1)
    expect(grouped[0].studentNames).toEqual(['Solo Student'])
  })
})

describe('deriveGroupStatus', () => {
  it('returns Scheduled for empty array', () => {
    expect(deriveGroupStatus([])).toBe('Scheduled')
  })

  it('returns the status when all lessons have same status', () => {
    const scheduled = [createLesson({ status: 'Scheduled' }), createLesson({ status: 'Scheduled' })]
    expect(deriveGroupStatus(scheduled)).toBe('Scheduled')

    const completed = [createLesson({ status: 'Completed' }), createLesson({ status: 'Completed' })]
    expect(deriveGroupStatus(completed)).toBe('Completed')

    const cancelled = [createLesson({ status: 'Cancelled' }), createLesson({ status: 'Cancelled' })]
    expect(deriveGroupStatus(cancelled)).toBe('Cancelled')
  })

  it('returns Cancelled if any lesson is cancelled (teacher cancellation)', () => {
    const lessons = [
      createLesson({ status: 'Scheduled' }),
      createLesson({ status: 'Cancelled' }),
      createLesson({ status: 'Completed' }),
    ]

    expect(deriveGroupStatus(lessons)).toBe('Cancelled')
  })

  it('returns NoShow if any lesson is NoShow (and none cancelled)', () => {
    const lessons = [
      createLesson({ status: 'Scheduled' }),
      createLesson({ status: 'NoShow' }),
      createLesson({ status: 'Completed' }),
    ]

    expect(deriveGroupStatus(lessons)).toBe('NoShow')
  })

  it('returns Completed if any lesson is completed (and none cancelled/noshow)', () => {
    const lessons = [createLesson({ status: 'Scheduled' }), createLesson({ status: 'Completed' })]

    expect(deriveGroupStatus(lessons)).toBe('Completed')
  })
})

describe('isGroupLesson', () => {
  it('returns false for single lesson', () => {
    expect(isGroupLesson([createLesson()])).toBe(false)
  })

  it('returns true for multiple lessons', () => {
    expect(isGroupLesson([createLesson(), createLesson()])).toBe(true)
  })

  it('returns false for empty array', () => {
    expect(isGroupLesson([])).toBe(false)
  })
})
