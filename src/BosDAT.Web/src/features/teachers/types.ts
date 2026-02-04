// Teacher Domain Types

import type { Instrument } from '@/features/instruments/types'
import type { CourseTypeSimple } from '@/features/course-types/types'
import type { DayOfWeek } from '@/lib/datetime-helpers'

// Re-export for consumers
export type { DayOfWeek }

export type TeacherRole = 'Teacher' | 'Admin' | 'Staff'

export interface Teacher {
  id: string
  firstName: string
  lastName: string
  prefix?: string
  fullName: string
  email: string
  phone?: string
  address?: string
  postalCode?: string
  city?: string
  hourlyRate: number
  isActive: boolean
  role: TeacherRole
  notes?: string
  instruments: Instrument[]
  courseTypes: CourseTypeSimple[]
  createdAt: string
  updatedAt: string
}

export interface TeacherList {
  id: string
  fullName: string
  email: string
  phone?: string
  isActive: boolean
  role: TeacherRole
  instruments: string[]
  courseTypes: string[]
}

export interface CreateTeacher {
  firstName: string
  lastName: string
  prefix?: string
  email: string
  phone?: string
  address?: string
  postalCode?: string
  city?: string
  hourlyRate: number
  role?: TeacherRole
  notes?: string
  isActive: boolean
  instrumentIds: number[]
  courseTypeIds: string[]
}

export interface TeacherAvailability {
  id: string
  dayOfWeek: DayOfWeek // "Sunday", "Monday", etc.
  fromTime: string // "HH:mm:ss"
  untilTime: string // "HH:mm:ss"
}

export interface UpdateTeacherAvailability {
  dayOfWeek: number
  fromTime: string
  untilTime: string
}
