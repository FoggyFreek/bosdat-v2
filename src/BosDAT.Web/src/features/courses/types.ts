// Course Domain Types

import type { Enrollment } from '@/features/enrollments/types'
import { DayOfWeek } from '@/lib/datetime-helpers'

export type CourseStatus = 'Active' | 'Paused' | 'Completed' | 'Cancelled'
// needed to properly translate Typescript Type
export const courseStatusTranslations = {
    'Active': 'courses.status.active',
    'Paused': 'courses.status.paused',
    'Completed': 'courses.status.completed',
    'Cancelled': 'courses.status.cancelled',
  } as const satisfies Record<CourseStatus, string>;

  
export type CourseFrequency = 'Once' | 'Weekly' | 'Biweekly'
// needed to properly translate Typescript Type
export const courseFrequencyTranslations = {
    'Once': 'courses.frequency.once',
    'Weekly': 'courses.frequency.weekly',
    'Biweekly': 'courses.frequency.biweekly',
  } as const satisfies Record<CourseFrequency, string>;

export type WeekParity = 'All' | 'Odd' | 'Even'
export const weekParityTranslations = {
    'All': 'courses.parity.all',
    'Odd': 'courses.parity.odd',
    'Even': 'courses.parity.even',
  } as const satisfies Record<WeekParity, string>;

export interface Course {
  id: string
  teacherId: string
  teacherName: string
  courseTypeId: string
  courseTypeName: string
  instrumentName: string
  roomId?: number
  roomName?: string
  dayOfWeek: DayOfWeek
  startTime: string
  endTime: string
  frequency: CourseFrequency
  weekParity: WeekParity
  has53WeekYearWarning?: boolean
  startDate: string
  endDate?: string
  status: CourseStatus
  isWorkshop: boolean
  isTrial: boolean
  notes?: string
  enrollmentCount: number
  enrollments: Enrollment[]
  createdAt: string
  updatedAt: string
}

export interface CourseList {
  id: string
  teacherName: string
  courseTypeName: string
  instrumentName: string
  roomName?: string
  dayOfWeek: DayOfWeek
  startTime: string
  endTime: string
  frequency: CourseFrequency
  weekParity: WeekParity
  status: CourseStatus
  enrollmentCount: number
}

export interface CreateCourse {
  teacherId: string
  courseTypeId: string
  roomId: number
  dayOfWeek: DayOfWeek
  startTime: string
  endTime: string
  frequency: CourseFrequency
  weekParity?: WeekParity
  startDate: string
  endDate?: string
  isTrial: boolean
}
