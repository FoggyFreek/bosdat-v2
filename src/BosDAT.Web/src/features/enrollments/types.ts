// Enrollment Domain Types
import type { InvoicingPreference } from '@/features/students/types'

export type EnrollmentStatus = 'Trail' | 'Active' | 'Withdrawn' | 'Completed' | 'Suspended'

export type DiscountType = 'None' | 'Family' | 'Course'

export type { InvoicingPreference } from '@/features/students/types'

// Enrollment Stepper Form Types

export type RecurrenceType = 'Trail' | 'Weekly' | 'Biweekly'

export interface Step1LessonDetailsData {
  courseTypeId: string | null
  teacherId: string | null
  startDate: string | null
  endDate: string | null
  isTrial: boolean
  recurrence: RecurrenceType
}

export interface EnrollmentFormData {
  step1: Step1LessonDetailsData
  step2: Step2StudentSelectionData
  step3: Step3CalendarSlotSelectionData
  step4: Record<string, unknown>
}

export const initialStep1Data: Step1LessonDetailsData = {
  courseTypeId: null,
  teacherId: null,
  startDate: null,
  endDate: null,
  isTrial: false,
  recurrence: 'Weekly',
}

// Step 2: Student Selection Types

export interface EnrollmentGroupMember {
  studentId: string
  studentName: string
  enrolledAt: string // ISO date, defaults to course start date
  discountType: DiscountType
  discountPercentage: number // from settings
  invoicingPreference: InvoicingPreference // Monthly or Quarterly
  note: string
  isEligibleForCourseDiscount: boolean // determined by checking active enrollments
}

export interface Step2StudentSelectionData {
  students: EnrollmentGroupMember[]
}

export const initialStep2Data: Step2StudentSelectionData = {
  students: [],
}

// Step 3: Calendar Slot Selection Types

export interface Step3CalendarSlotSelectionData {
  selectedRoomId: number | null
  selectedDayOfWeek: number | null  // 0-6
  selectedDate: string | null       // ISO date
  selectedStartTime: string | null  // HH:mm
  selectedEndTime: string | null    // HH:mm
}

export interface Step3ValidationResult {
  isValid: boolean
  errors: string[]
}

export interface Step2ValidationResult {
  isValid: boolean
  errors: string[]
}

export const initialStep3Data: Step3CalendarSlotSelectionData = {
  selectedRoomId: null,
  selectedDayOfWeek: null,
  selectedDate: null,
  selectedStartTime: null,
  selectedEndTime: null,
}

export const initialEnrollmentFormData: EnrollmentFormData = {
  step1: initialStep1Data,
  step2: initialStep2Data,
  step3: initialStep3Data,
  step4: {},
}

export interface Enrollment {
  id: string
  studentId: string
  studentName: string
  courseId: string
  enrolledAt: string
  discountPercent: number
  discountType: DiscountType
  status: EnrollmentStatus
  invoicingPreference: InvoicingPreference
  notes?: string
}

export interface CreateEnrollment {
  studentId: string
  courseId: string
  discountPercent?: number
  discountType?: DiscountType
  invoicingPreference?: InvoicingPreference
  notes?: string
}

export interface UpdateEnrollment {
  discountPercent: number
  status: EnrollmentStatus
  invoicingPreference: InvoicingPreference
  notes?: string
}

// Enrollment Validation & Conflict Types

export interface ConflictingCourse {
  courseId: string
  courseName: string
  dayOfWeek: string
  timeSlot: string
  frequency: string
  weekParity?: string
}

export interface ValidateEnrollmentRequest {
  studentId: string
  courseId: string
}
