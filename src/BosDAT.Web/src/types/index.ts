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
  billingContactName?: string
  billingContactEmail?: string
  billingContactPhone?: string
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
  billingContactName?: string
  billingContactEmail?: string
  billingContactPhone?: string
  billingAddress?: string
  billingPostalCode?: string
  billingCity?: string
  autoDebit?: boolean
  notes?: string
}

// Duplicate detection types
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

// Lesson types
export type LessonStatus = 'Scheduled' | 'Completed' | 'Cancelled' | 'NoShow'

export interface Lesson {
  id: string
  courseId: string
  studentId?: string
  studentName?: string
  teacherId: string
  teacherName: string
  roomId?: number
  roomName?: string
  lessonTypeName: string
  instrumentName: string
  scheduledDate: string
  startTime: string
  endTime: string
  status: LessonStatus
  cancellationReason?: string
  isInvoiced: boolean
  isPaidToTeacher: boolean
  notes?: string
  createdAt: string
  updatedAt: string
}

export interface CreateLesson {
  courseId: string
  studentId?: string
  teacherId: string
  roomId?: number
  scheduledDate: string
  startTime: string
  endTime: string
  notes?: string
}

export interface UpdateLesson {
  studentId?: string
  teacherId: string
  roomId?: number
  scheduledDate: string
  startTime: string
  endTime: string
  status: LessonStatus
  cancellationReason?: string
  notes?: string
}

export interface GenerateLessons {
  courseId: string
  startDate: string
  endDate: string
  skipHolidays?: boolean
}

export interface GenerateLessonsResult {
  courseId: string
  startDate: string
  endDate: string
  lessonsCreated: number
  lessonsSkipped: number
}

export interface BulkGenerateLessons {
  startDate: string
  endDate: string
  skipHolidays?: boolean
}

export interface BulkGenerateLessonsResult {
  startDate: string
  endDate: string
  totalCoursesProcessed: number
  totalLessonsCreated: number
  totalLessonsSkipped: number
  courseResults: GenerateLessonsResult[]
}

// Calendar types
export interface CalendarLesson {
  id: string
  title: string
  date: string
  startTime: string
  endTime: string
  studentName?: string
  teacherName: string
  roomName?: string
  instrumentName: string
  status: LessonStatus
}

export interface Holiday {
  id: number
  name: string
  startDate: string
  endDate: string
}

export interface WeekCalendar {
  weekStart: string
  weekEnd: string
  lessons: CalendarLesson[]
  holidays: Holiday[]
}

export interface DayCalendar {
  date: string
  dayOfWeek: number
  lessons: CalendarLesson[]
  isHoliday: boolean
  holidayName?: string
}

export interface MonthCalendar {
  year: number
  month: number
  monthStart: string
  monthEnd: string
  lessonsByDate: Record<string, CalendarLesson[]>
  holidays: Holiday[]
  totalLessons: number
}

export interface Availability {
  date: string
  startTime: string
  endTime: string
  isAvailable: boolean
  conflicts: Conflict[]
}

export interface Conflict {
  type: string
  description: string
}

// Student Enrollment types for detail views
export interface StudentEnrollment {
  id: string
  courseId: string
  instrumentName: string
  lessonTypeName: string
  teacherName: string
  roomName?: string
  dayOfWeek: number
  startTime: string
  endTime: string
  enrolledAt: string
  discountPercent: number
  status: EnrollmentStatus
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
