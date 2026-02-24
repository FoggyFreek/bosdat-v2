// Absence Domain Types

export type AbsenceReason = 'Holiday' | 'Sick' | 'Other'

export interface Absence {
  id: string
  studentId?: string
  teacherId?: string
  personName?: string
  startDate: string
  endDate: string
  reason: AbsenceReason
  notes?: string
  invoiceLesson: boolean
  affectedLessonsCount: number
}

export interface CreateAbsence {
  studentId?: string
  teacherId?: string
  startDate: string
  endDate: string
  reason: AbsenceReason
  notes?: string
  invoiceLesson: boolean
}

export interface UpdateAbsence {
  startDate: string
  endDate: string
  reason: AbsenceReason
  notes?: string
  invoiceLesson: boolean
}

// Translation Mappings
export const absenceReasonTranslations = {
  Holiday: 'absences.reason.holiday',
  Sick: 'absences.reason.sick',
  Other: 'absences.reason.other',
} as const satisfies Record<AbsenceReason, string>
