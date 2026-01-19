// Auth types
export interface LoginDto {
  email: string
  password: string
}

export interface RegisterDto {
  email: string
  password: string
  firstName?: string
  lastName?: string
}

export interface AuthResponse {
  token: string
  refreshToken: string
  expiresAt: string
  user: User
}

export interface User {
  id: string
  email: string
  firstName?: string
  lastName?: string
  roles: string[]
}

// Student types
export type StudentStatus = 'Active' | 'Inactive' | 'Trial'
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
  billingAddress?: string
  billingPostalCode?: string
  billingCity?: string
  autoDebit: boolean
  notes?: string
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
  billingAddress?: string
  billingPostalCode?: string
  billingCity?: string
  autoDebit?: boolean
  notes?: string
}

// Teacher types
export type TeacherRole = 'Teacher' | 'Admin' | 'Staff'

export interface Teacher {
  id: string
  firstName: string
  lastName: string
  prefix?: string
  fullName: string
  email: string
  phone?: string
  address?: string
  postalCode?: string
  city?: string
  hourlyRate: number
  isActive: boolean
  role: TeacherRole
  notes?: string
  instruments: Instrument[]
  createdAt: string
  updatedAt: string
}

export interface TeacherList {
  id: string
  fullName: string
  email: string
  phone?: string
  isActive: boolean
  role: TeacherRole
  instruments: string[]
}

export interface CreateTeacher {
  firstName: string
  lastName: string
  prefix?: string
  email: string
  phone?: string
  address?: string
  postalCode?: string
  city?: string
  hourlyRate: number
  role?: TeacherRole
  notes?: string
  instrumentIds: number[]
}

// Instrument types
export type InstrumentCategory = 'String' | 'Percussion' | 'Vocal' | 'Keyboard' | 'Wind' | 'Brass' | 'Electronic' | 'Other'

export interface Instrument {
  id: number
  name: string
  category: InstrumentCategory
  isActive: boolean
}

// Room types
export interface Room {
  id: number
  name: string
  capacity: number
  hasPiano: boolean
  hasDrums: boolean
  hasAmplifier: boolean
  hasMicrophone: boolean
  hasWhiteboard: boolean
  isActive: boolean
  notes?: string
}

// Lesson Type
export type LessonTypeCategory = 'Individual' | 'Group' | 'Workshop'

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
}

// Course types
export type CourseStatus = 'Active' | 'Paused' | 'Completed' | 'Cancelled'
export type CourseFrequency = 'Weekly' | 'Biweekly' | 'Monthly'

export interface Course {
  id: string
  teacherId: string
  teacherName: string
  lessonTypeId: number
  lessonTypeName: string
  instrumentName: string
  roomId?: number
  roomName?: string
  dayOfWeek: number
  startTime: string
  endTime: string
  frequency: CourseFrequency
  startDate: string
  endDate?: string
  status: CourseStatus
  isWorkshop: boolean
  isTrial: boolean
  notes?: string
  enrollmentCount: number
  enrollments: Enrollment[]
  createdAt: string
  updatedAt: string
}

export interface CourseList {
  id: string
  teacherName: string
  lessonTypeName: string
  instrumentName: string
  roomName?: string
  dayOfWeek: number
  startTime: string
  endTime: string
  status: CourseStatus
  enrollmentCount: number
}

// Enrollment types
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

// API response types
export interface ApiError {
  message: string
  errors?: Record<string, string[]>
}

export interface PaginatedResponse<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}
