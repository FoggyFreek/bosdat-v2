// Schedule/Calendar Domain Types

import type { LessonStatus } from '@/features/lessons/types'

export interface CalendarLesson {
  id: string
  courseId: string
  studentId?: string
  title: string
  date: string
  frequency: string
  startTime: string
  endTime: string
  studentName?: string
  teacherName: string
  isTrial: boolean
  isWorkshop: boolean
  roomName?: string
  instrumentName: string
  status: LessonStatus
}

export interface Holiday {
  id: number
  name: string
  startDate: string
  endDate: string
}

export interface WeekCalendar {
  weekStart: string
  weekEnd: string
  lessons: CalendarLesson[]
  holidays: Holiday[]
}

export interface DayCalendar {
  date: string
  dayOfWeek: number
  lessons: CalendarLesson[]
  isHoliday: boolean
  holidayName?: string
}

export interface MonthCalendar {
  year: number
  month: number
  monthStart: string
  monthEnd: string
  lessonsByDate: Record<string, CalendarLesson[]>
  holidays: Holiday[]
  totalLessons: number
}

export interface Availability {
  date: string
  startTime: string
  endTime: string
  isAvailable: boolean
  conflicts: Conflict[]
}

export interface Conflict {
  type: string
  description: string
}
