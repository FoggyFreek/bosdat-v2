// Teacher Domain Types

import type { Instrument } from '@/features/instruments/types'
import type { LessonTypeSimple } from '@/features/lesson-types/types'

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
  lessonTypes: LessonTypeSimple[]
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
  lessonTypes: string[]
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
  lessonTypeIds: number[]
}
