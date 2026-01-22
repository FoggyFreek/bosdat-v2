// LessonType Domain Types

export type LessonTypeCategory = 'Individual' | 'Group' | 'Workshop'

export interface LessonTypeSimple {
  id: number
  name: string
  instrumentId: number
  instrumentName: string
  durationMinutes: number
  type: LessonTypeCategory
}

export interface LessonType {
  id: number
  instrumentId: number
  instrumentName: string
  name: string
  durationMinutes: number
  type: LessonTypeCategory
  priceAdult: number
  priceChild: number
  maxStudents: number
  isActive: boolean
  activeCourseCount: number
  hasTeachersForLessonType: boolean
}

export interface CreateLessonType {
  instrumentId: number
  name: string
  durationMinutes: number
  type: LessonTypeCategory
  priceAdult: number
  priceChild: number
  maxStudents: number
}

export interface UpdateLessonType {
  instrumentId: number
  name: string
  durationMinutes: number
  type: LessonTypeCategory
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
