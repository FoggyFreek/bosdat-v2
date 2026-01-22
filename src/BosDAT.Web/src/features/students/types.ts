// Student Domain Types

export type StudentStatus = 'Active' | 'Inactive' | 'Trial'
export type Gender = 'Male' | 'Female' | 'Other' | 'PreferNotToSay'

export interface Student {
  id: string
  firstName: string
  lastName: string
  prefix?: string
  fullName: string
  email: string
  phone?: string
  phoneAlt?: string
  address?: string
  postalCode?: string
  city?: string
  dateOfBirth?: string
  gender?: Gender
  status: StudentStatus
  enrolledAt?: string
  billingContactName?: string
  billingContactEmail?: string
  billingContactPhone?: string
  billingAddress?: string
  billingPostalCode?: string
  billingCity?: string
  autoDebit: boolean
  notes?: string
  createdAt: string
  updatedAt: string
}

export interface StudentList {
  id: string
  fullName: string
  email: string
  phone?: string
  status: StudentStatus
  enrolledAt?: string
}

export interface CreateStudent {
  firstName: string
  lastName: string
  prefix?: string
  email: string
  phone?: string
  phoneAlt?: string
  address?: string
  postalCode?: string
  city?: string
  dateOfBirth?: string
  gender?: Gender
  status?: StudentStatus
  billingContactName?: string
  billingContactEmail?: string
  billingContactPhone?: string
  billingAddress?: string
  billingPostalCode?: string
  billingCity?: string
  autoDebit?: boolean
  notes?: string
}

// Duplicate Detection Types
export interface CheckDuplicatesDto {
  firstName: string
  lastName: string
  email: string
  phone?: string
  dateOfBirth?: string
  excludeId?: string
}

export interface DuplicateMatch {
  id: string
  fullName: string
  email: string
  phone?: string
  status: StudentStatus
  confidenceScore: number
  matchReason: string
}

export interface DuplicateCheckResult {
  hasDuplicates: boolean
  duplicates: DuplicateMatch[]
}

// Student Enrollment view types
import type { EnrollmentStatus } from '@/features/enrollments/types'

export interface StudentEnrollment {
  id: string
  courseId: string
  instrumentName: string
  lessonTypeName: string
  teacherName: string
  roomName?: string
  dayOfWeek: number
  startTime: string
  endTime: string
  enrolledAt: string
  discountPercent: number
  status: EnrollmentStatus
}
