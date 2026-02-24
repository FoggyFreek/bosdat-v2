import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { TeacherAbsenceSection } from '../TeacherAbsenceSection'
import { absencesApi } from '@/features/absences/api'
import type { Absence } from '@/features/absences/types'

vi.mock('@/features/absences/api', () => ({
  absencesApi: {
    getByTeacher: vi.fn(),
    getByStudent: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
  },
}))

describe('TeacherAbsenceSection', () => {
  const mockTeacherId = 'teacher-456'

  const mockAbsence: Absence = {
    id: 'absence-2',
    teacherId: mockTeacherId,
    personName: 'Jane Teacher',
    startDate: '2026-04-10',
    endDate: '2026-04-14',
    reason: 'Holiday',
    notes: 'Spring break',
    invoiceLesson: false,
    affectedLessonsCount: 5,
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders teacher absences', async () => {
    vi.mocked(absencesApi.getByTeacher).mockResolvedValue([mockAbsence])

    render(<TeacherAbsenceSection teacherId={mockTeacherId} />)

    await waitFor(() => {
      expect(screen.getByText('2026-04-10 — 2026-04-14')).toBeInTheDocument()
    })
    expect(screen.getByText('absences.reason.holiday')).toBeInTheDocument()
    expect(screen.getByText('Spring break')).toBeInTheDocument()
  })

  it('renders empty state when no absences', async () => {
    vi.mocked(absencesApi.getByTeacher).mockResolvedValue([])

    render(<TeacherAbsenceSection teacherId={mockTeacherId} />)

    await waitFor(() => {
      expect(screen.getByText('students.absence.noAbsences')).toBeInTheDocument()
    })
  })

  it('shows add absence button', async () => {
    vi.mocked(absencesApi.getByTeacher).mockResolvedValue([])

    render(<TeacherAbsenceSection teacherId={mockTeacherId} />)

    expect(screen.getByText('students.absence.addAbsence')).toBeInTheDocument()
  })

  it('opens dialog when add button is clicked', async () => {
    const user = userEvent.setup()
    vi.mocked(absencesApi.getByTeacher).mockResolvedValue([])

    render(<TeacherAbsenceSection teacherId={mockTeacherId} />)

    await user.click(screen.getByText('students.absence.addAbsence'))

    await waitFor(() => {
      expect(screen.getByText('students.absence.startDate')).toBeInTheDocument()
    })
  })

  it('renders multiple absences', async () => {
    const secondAbsence: Absence = {
      id: 'absence-3',
      teacherId: mockTeacherId,
      personName: 'Jane Teacher',
      startDate: '2026-05-01',
      endDate: '2026-05-02',
      reason: 'Sick',
      invoiceLesson: false,
      affectedLessonsCount: 1,
    }

    vi.mocked(absencesApi.getByTeacher).mockResolvedValue([mockAbsence, secondAbsence])

    render(<TeacherAbsenceSection teacherId={mockTeacherId} />)

    await waitFor(() => {
      expect(screen.getByText('2026-04-10 — 2026-04-14')).toBeInTheDocument()
      expect(screen.getByText('2026-05-01 — 2026-05-02')).toBeInTheDocument()
    })
  })
})
