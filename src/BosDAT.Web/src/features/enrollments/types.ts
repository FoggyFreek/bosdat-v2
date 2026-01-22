// Enrollment Domain Types

export type EnrollmentStatus = 'Active' | 'Withdrawn' | 'Completed' | 'Suspended'

export interface Enrollment {
  id: string
  studentId: string
  studentName: string
  courseId: string
  enrolledAt: string
  discountPercent: number
  status: EnrollmentStatus
  notes?: string
}

export interface CreateEnrollment {
  studentId: string
  courseId: string
  discountPercent?: number
  notes?: string
}

export interface UpdateEnrollment {
  discountPercent: number
  status: EnrollmentStatus
  notes?: string
}
