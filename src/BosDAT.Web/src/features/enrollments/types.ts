// Enrollment Domain Types

export type EnrollmentStatus = 'Trail' | 'Active' | 'Withdrawn' | 'Completed' | 'Suspended'

export type DiscountType = 'None' | 'Family' | 'Course'

// Enrollment Stepper Form Types

export type RecurrenceType = 'Weekly' | 'Biweekly'

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
  step3: Record<string, unknown>
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
  note: string
  isEligibleForCourseDiscount: boolean // determined by checking active enrollments
}

export interface Step2StudentSelectionData {
  students: EnrollmentGroupMember[]
}

export const initialStep2Data: Step2StudentSelectionData = {
  students: [],
}

export const initialEnrollmentFormData: EnrollmentFormData = {
  step1: initialStep1Data,
  step2: initialStep2Data,
  step3: {},
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
  notes?: string
}

export interface CreateEnrollment {
  studentId: string
  courseId: string
  discountPercent?: number
  discountType?: DiscountType
  notes?: string
}

export interface UpdateEnrollment {
  discountPercent: number
  status: EnrollmentStatus
  notes?: string
}
