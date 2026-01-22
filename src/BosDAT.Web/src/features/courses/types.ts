// Course Domain Types

import type { Enrollment } from '@/features/enrollments/types'

export type CourseStatus = 'Active' | 'Paused' | 'Completed' | 'Cancelled'
export type CourseFrequency = 'Weekly' | 'Biweekly' | 'Monthly'

export interface Course {
  id: string
  teacherId: string
  teacherName: string
  lessonTypeId: number
  lessonTypeName: string
  instrumentName: string
  roomId?: number
  roomName?: string
  dayOfWeek: number
  startTime: string
  endTime: string
  frequency: CourseFrequency
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
  lessonTypeName: string
  instrumentName: string
  roomName?: string
  dayOfWeek: number
  startTime: string
  endTime: string
  status: CourseStatus
  enrollmentCount: number
}
