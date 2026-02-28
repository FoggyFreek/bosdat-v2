import type { ReactNode } from 'react'

export type SettingKey =
  | 'profile'
  | 'preferences'
  | 'instruments'
  | 'course-types'
  | 'rooms'
  | 'holidays'
  | 'scheduling'
  | 'invoice-generation'
  | 'system'
  | 'seeding'
  | 'manage-users'

export interface NavItem {
  key: SettingKey
  label: string
  icon: ReactNode
}

export interface NavGroup {
  label: string
  items: NavItem[]
}

export interface SystemSetting {
  key: string
  value: string
  type?: string
  description?: string
}

export interface SeederStatusResponse {
  isSeeded: boolean
  environment: string
  canSeed: boolean
  canReset: boolean
}

export interface SeederActionResponse {
  success: boolean
  message: string
  action: string
}

export interface SchedulingStatus {
  lastScheduledDate: string | null
  daysAhead: number
  activeCourseCount: number
}

export interface ScheduleRun {
  id: string
  startDate: string
  endDate: string
  totalCoursesProcessed: number
  totalLessonsCreated: number
  totalLessonsSkipped: number
  skipHolidays: boolean
  status: string
  errorMessage: string | null
  initiatedBy: string
  createdAt: string
}

export interface ScheduleRunsResponse {
  items: ScheduleRun[]
  totalCount: number
  page: number
  pageSize: number
}

export interface ManualRunResult {
  scheduleRunId: string
  startDate: string
  endDate: string
  totalCoursesProcessed: number
  totalLessonsCreated: number
  totalLessonsSkipped: number
  status: string
}

export interface InvoiceRun {
  id: string
  periodStart: string
  periodEnd: string
  periodType: string
  totalEnrollmentsProcessed: number
  totalInvoicesGenerated: number
  totalSkipped: number
  totalFailed: number
  totalAmount: number
  durationMs: number
  status: string
  errorMessage: string | null
  initiatedBy: string
  createdAt: string
}

export interface InvoiceRunsResponse {
  items: InvoiceRun[]
  totalCount: number
  page: number
  pageSize: number
}

export interface StartInvoiceRunRequest {
  periodStart: string
  periodEnd: string
  periodType: string
  applyLedgerCorrections: boolean
}

export interface InvoiceRunResult {
  invoiceRunId: string
  periodStart: string
  periodEnd: string
  periodType: string
  totalEnrollmentsProcessed: number
  totalInvoicesGenerated: number
  totalSkipped: number
  totalFailed: number
  totalAmount: number
  durationMs: number
  status: string
}
