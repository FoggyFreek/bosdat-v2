import type { CalendarLesson } from '../types'
import type { LessonStatus } from '@/features/lessons/types'

export interface GroupedLesson {
  courseId: string
  date: string
  startTime: string
  endTime: string
  title: string
  teacherName: string
  roomName?: string
  instrumentName: string
  status: LessonStatus
  lessons: CalendarLesson[]
  studentNames: string[]
}

const createGroupKey = (courseId: string, date: string): string => `${courseId}:${date}`

export const groupLessonsByCourseAndDate = (lessons: CalendarLesson[]): GroupedLesson[] => {
  const groups = new Map<string, CalendarLesson[]>()

  for (const lesson of lessons) {
    const key = createGroupKey(lesson.courseId, lesson.date)
    const existing = groups.get(key) ?? []
    groups.set(key, [...existing, lesson])
  }

  return Array.from(groups.values()).map((groupLessons) => {
    const first = groupLessons[0]
    const studentNames = groupLessons
      .map((l) => l.studentName)
      .filter((name): name is string => name != null)

    return {
      courseId: first.courseId,
      date: first.date,
      startTime: first.startTime,
      endTime: first.endTime,
      title: first.instrumentName,
      teacherName: first.teacherName,
      roomName: first.roomName,
      instrumentName: first.instrumentName,
      status: deriveGroupStatus(groupLessons),
      lessons: groupLessons,
      studentNames,
    }
  })
}

export const deriveGroupStatus = (lessons: CalendarLesson[]): LessonStatus => {
  if (lessons.length === 0) {
    return 'Scheduled'
  }

  const statuses = lessons.map((l) => l.status)

  // If all lessons have the same status, return that status
  const uniqueStatuses = [...new Set(statuses)]
  if (uniqueStatuses.length === 1) {
    return uniqueStatuses[0]
  }

  // Priority: Cancelled > NoShow > Completed > Scheduled
  // If any are cancelled, the group is considered cancelled (teacher cancellation)
  if (statuses.includes('Cancelled')) {
    return 'Cancelled'
  }
  if (statuses.includes('NoShow')) {
    return 'NoShow'
  }
  if (statuses.includes('Completed')) {
    return 'Completed'
  }

  return 'Scheduled'
}

export const isGroupLesson = (lessons: CalendarLesson[]): boolean => {
  return lessons.length > 1
}
