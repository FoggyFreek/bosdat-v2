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
  | 'transactions'

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

// Invoice Types
export type InvoiceStatus = 'Draft' | 'Sent' | 'Paid' | 'Overdue' | 'Cancelled'
export type InvoicingPreference = 'Monthly' | 'Quarterly'
export type PaymentMethod = 'Cash' | 'Bank' | 'Card' | 'DirectDebit' | 'Other' | 'CreditBalance'

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

export interface InvoicePayment {
  id: string
  invoiceId: string
  amount: number
  paymentDate: string
  method: PaymentMethod
  reference?: string
  notes?: string
  recordedByName?: string
  createdAt?: string
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
  status: InvoiceStatus
  paidAt?: string
  paymentMethod?: string
  notes?: string
  lines: InvoiceLine[]
  payments: InvoicePayment[]
  amountPaid: number
  balance: number
  createdAt: string
  updatedAt: string
  isCreditInvoice: boolean
  originalInvoiceId?: string
  originalInvoiceNumber?: string
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
  isCreditInvoice: boolean
  originalInvoiceId?: string
  originalInvoiceNumber?: string
}

export interface CreateCreditInvoice {
  selectedLineIds: number[]
  notes?: string
}

export interface GenerateInvoice {
  enrollmentId: string
  periodStart: string
  periodEnd: string
}

export interface GenerateBatchInvoices {
  periodStart: string
  periodEnd: string
  periodType: InvoicingPreference
}

export interface SchoolBillingInfo {
  name: string
  address?: string
  postalCode?: string
  city?: string
  phone?: string
  email?: string
  kvkNumber?: string
  btwNumber?: string
  iban?: string
  vatRate: number
}

export interface InvoicePrintData {
  invoice: Invoice
  schoolInfo: SchoolBillingInfo
}

// Student Transaction Types
export type TransactionType =
  | 'InvoiceCharge'
  | 'Payment'
  | 'InvoiceCancellation'
  | 'InvoiceAdjustment'
  | 'CreditInvoice'
  | 'CorrectionApplied'

export interface StudentTransaction {
  id: string
  studentId: string
  transactionDate: string
  type: TransactionType
  description: string
  referenceNumber: string
  debit: number
  credit: number
  runningBalance: number
  invoiceId?: string
  paymentId?: string
  createdAt: string
  createdByName: string
}

export const transactionTypeTranslations = {
  InvoiceCharge: 'students.transactions.type.invoiceCharge',
  Payment: 'students.transactions.type.payment',
  InvoiceCancellation: 'students.transactions.type.invoiceCancellation',
  InvoiceAdjustment: 'students.transactions.type.invoiceAdjustment',
  CreditInvoice: 'students.transactions.type.creditInvoice',
  CorrectionApplied: 'students.transactions.type.correctionApplied',
} as const satisfies Record<TransactionType, string>

export interface RecordPayment {
  amount: number
  paymentDate: string
  method: PaymentMethod
  reference?: string
  notes?: string
}

// Translation Mappings
export const studentStatusTranslations = {
  Active: 'students.status.active',
  Inactive: 'students.status.inactive',
  Trial: 'students.status.trial',
} as const satisfies Record<StudentStatus, string>

export const genderTranslations = {
  Male: 'students.gender.male',
  Female: 'students.gender.female',
  Other: 'students.gender.other',
  PreferNotToSay: 'students.gender.preferNotToSay',
} as const satisfies Record<Gender, string>

export const invoiceStatusTranslations = {
  Draft: 'students.invoices.status.draft',
  Sent: 'students.invoices.status.sent',
  Paid: 'students.invoices.status.paid',
  Overdue: 'students.invoices.status.overdue',
  Cancelled: 'students.invoices.status.cancelled',
} as const satisfies Record<InvoiceStatus, string>

export const invoicingPreferenceTranslations = {
  Monthly: 'students.invoicingPreference.monthly',
  Quarterly: 'students.invoicingPreference.quarterly',
} as const satisfies Record<InvoicingPreference, string>

export const paymentMethodTranslations = {
  Cash: 'students.paymentMethod.cash',
  Bank: 'students.paymentMethod.bank',
  Card: 'students.paymentMethod.card',
  DirectDebit: 'students.paymentMethod.directDebit',
  Other: 'students.paymentMethod.other',
  CreditBalance: 'students.paymentMethod.creditBalance',
} as const satisfies Record<PaymentMethod, string>

