// CourseType Domain Types

export type CourseTypeCategory = 'Individual' | 'Group' | 'Workshop'

export interface CourseTypeSimple {
  id: number
  name: string
  instrumentId: number
  instrumentName: string
  durationMinutes: number
  type: CourseTypeCategory
}

export interface CourseType {
  id: number
  instrumentId: number
  instrumentName: string
  name: string
  durationMinutes: number
  type: CourseTypeCategory
  priceAdult: number
  priceChild: number
  maxStudents: number
  isActive: boolean
  activeCourseCount: number
  hasTeachersForCourseType: boolean
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
  priceAdult: number
  priceChild: number
  maxStudents: number
  isActive: boolean
}

export interface TeacherAvailabilityForInstrument {
  instrumentId: number
  instrumentName: string
  teacherCount: number
  hasTeachers: boolean
}
