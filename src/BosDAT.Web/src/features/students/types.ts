// Student Domain Types
import type { ReactNode } from 'react'

export type StudentStatus = 'Active' | 'Inactive' | 'Trial'

// Student Detail Navigation Types
export type StudentSectionKey =
  | 'profile'
  | 'preferences'
  | 'enrollments'
  | 'lessons'
  | 'absence'
  | 'invoices'
  | 'corrections'
  | 'balance'

export interface StudentNavItem {
  key: StudentSectionKey
  label: string
  icon: ReactNode
}

export interface StudentNavGroup {
  label: string
  items: StudentNavItem[]
}
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
  registrationFeePaidAt?: string
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

// Registration Fee Types
export interface RegistrationFeeStatus {
  hasPaid: boolean
  paidAt?: string
  amount?: number
  ledgerEntryId?: string
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
  courseTypeName: string
  teacherName: string
  roomName?: string
  dayOfWeek: number
  startTime: string
  endTime: string
  enrolledAt: string
  discountPercent: number
  status: EnrollmentStatus
}

// Student Ledger Types
export type LedgerEntryType = 'Credit' | 'Debit'
export type LedgerEntryStatus = 'Open' | 'Applied' | 'PartiallyApplied' | 'Reversed'

export interface LedgerApplication {
  id: string
  invoiceId: string
  invoiceNumber: string
  appliedAmount: number
  appliedAt: string
  appliedByName: string
}

export interface StudentLedgerEntry {
  id: string
  correctionRefName: string
  description: string
  studentId: string
  studentName: string
  courseId?: string
  courseName?: string
  amount: number
  entryType: LedgerEntryType
  status: LedgerEntryStatus
  appliedAmount: number
  remainingAmount: number
  createdAt: string
  createdByName: string
  applications: LedgerApplication[]
}

export interface StudentLedgerSummary {
  studentId: string
  studentName: string
  totalCredits: number
  totalDebits: number
  availableCredit: number
  openEntryCount: number
}

export interface CreateStudentLedgerEntry {
  description: string
  studentId: string
  courseId?: string
  amount: number
  entryType: LedgerEntryType
}

// Enrollment Pricing Types
export interface EnrollmentPricing {
  enrollmentId: string
  courseId: string
  courseName: string
  basePriceAdult: number
  basePriceChild: number
  isChildPricing: boolean
  applicableBasePrice: number
  discountPercent: number
  discountAmount: number
  pricePerLesson: number
}
