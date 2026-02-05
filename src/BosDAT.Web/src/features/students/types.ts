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
import { DayOfWeek } from '@/lib/datetime-helpers'

export interface StudentEnrollment {
  id: string
  courseId: string
  instrumentName: string
  courseTypeName: string
  teacherName: string
  roomName?: string
  dayOfWeek: DayOfWeek
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

export interface DecoupleApplicationResult {
  ledgerEntryId: string
  correctionRefName: string
  invoiceId: string
  invoiceNumber: string
  decoupledAmount: number
  newEntryStatus: LedgerEntryStatus
  newInvoiceStatus: string
  decoupledAt: string
  decoupledByName: string
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

// Invoice Types
export type InvoiceStatus = 'Draft' | 'Sent' | 'Paid' | 'Overdue' | 'Cancelled'
export type InvoicingPreference = 'Monthly' | 'Quarterly'
export type PaymentMethod = 'Cash' | 'Bank' | 'Card' | 'DirectDebit' | 'Other'

export interface BillingContact {
  name: string
  email?: string
  phone?: string
  address?: string
  postalCode?: string
  city?: string
}

export interface InvoiceLine {
  id: number
  lessonId?: string
  pricingVersionId?: string
  description: string
  quantity: number
  unitPrice: number
  vatRate: number
  lineTotal: number
  lessonDate?: string
  courseName?: string
}

export interface InvoiceLedgerApplication {
  id: string
  ledgerEntryId: string
  correctionRefName?: string
  description?: string
  appliedAmount: number
  appliedAt: string
  entryType: LedgerEntryType
}

export interface InvoicePayment {
  id: string
  invoiceId: string
  amount: number
  paymentDate: string
  method: PaymentMethod
  reference?: string
  notes?: string
}

export interface Invoice {
  id: string
  invoiceNumber: string
  studentId: string
  enrollmentId?: string
  studentName: string
  studentEmail: string
  issueDate: string
  dueDate: string
  description?: string
  periodStart?: string
  periodEnd?: string
  periodType?: InvoicingPreference
  subtotal: number
  vatAmount: number
  total: number
  discountAmount: number
  ledgerCreditsApplied: number
  ledgerDebitsApplied: number
  status: InvoiceStatus
  paidAt?: string
  paymentMethod?: string
  notes?: string
  lines: InvoiceLine[]
  payments: InvoicePayment[]
  ledgerApplications: InvoiceLedgerApplication[]
  amountPaid: number
  balance: number
  createdAt: string
  updatedAt: string
  billingContact?: BillingContact
}

export interface InvoiceListItem {
  id: string
  invoiceNumber: string
  studentName: string
  description?: string
  issueDate: string
  dueDate: string
  periodStart?: string
  periodEnd?: string
  total: number
  status: InvoiceStatus
  balance: number
}

export interface GenerateInvoice {
  enrollmentId: string
  periodStart: string
  periodEnd: string
  applyLedgerCorrections?: boolean
}

export interface GenerateBatchInvoices {
  periodStart: string
  periodEnd: string
  periodType: InvoicingPreference
  applyLedgerCorrections?: boolean
}

export interface SchoolBillingInfo {
  name: string
  address?: string
  postalCode?: string
  city?: string
  phone?: string
  email?: string
  kvkNumber?: string
  iban?: string
  vatRate: number
}

export interface InvoicePrintData {
  invoice: Invoice
  schoolInfo: SchoolBillingInfo
}
