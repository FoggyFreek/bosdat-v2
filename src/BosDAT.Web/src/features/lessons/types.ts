// Lesson Domain Types

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
  courseTypeName: string
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

// Translation Mappings
export const lessonStatusTranslations = {
  Scheduled: 'lessons.status.scheduled',
  Completed: 'lessons.status.completed',
  Cancelled: 'lessons.status.cancelled',
  NoShow: 'lessons.status.noShow',
} as const satisfies Record<LessonStatus, string>
