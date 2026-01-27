// CourseType Domain Types

export type CourseTypeCategory = 'Individual' | 'Group' | 'Workshop'

export interface CourseTypePricingVersion {
  id: string
  courseTypeId: string
  priceAdult: number
  priceChild: number
  validFrom: string
  validUntil: string | null
  isCurrent: boolean
  createdAt: string
}

export interface PricingEditability {
  canEditDirectly: boolean
  isInvoiced: boolean
  reason?: string
}

export interface CourseTypeSimple {
  id: string
  name: string
  instrumentId: number
  instrumentName: string
  durationMinutes: number
  type: CourseTypeCategory
}

export interface CourseType {
  id: string
  instrumentId: number
  instrumentName: string
  name: string
  durationMinutes: number
  type: CourseTypeCategory
  maxStudents: number
  isActive: boolean
  activeCourseCount: number
  hasTeachersForCourseType: boolean
  currentPricing: CourseTypePricingVersion | null
  pricingHistory: CourseTypePricingVersion[]
  canEditPricingDirectly: boolean
}

export interface CreateCourseType {
  instrumentId: number
  name: string
  durationMinutes: number
  type: CourseTypeCategory
  priceAdult: number
  priceChild: number
  maxStudents: number
}

export interface UpdateCourseType {
  instrumentId: number
  name: string
  durationMinutes: number
  type: CourseTypeCategory
  maxStudents: number
  isActive: boolean
}

export interface UpdateCourseTypePricing {
  priceAdult: number
  priceChild: number
}

export interface CreateCourseTypePricingVersion {
  priceAdult: number
  priceChild: number
  validFrom: string
}

export interface TeacherAvailabilityForInstrument {
  instrumentId: number
  instrumentName: string
  teacherCount: number
  hasTeachers: boolean
}
