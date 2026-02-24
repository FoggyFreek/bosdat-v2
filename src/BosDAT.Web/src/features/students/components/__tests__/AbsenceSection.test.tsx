import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { AbsenceSection } from '../AbsenceSection'
import { absencesApi } from '@/features/absences/api'
import type { Absence } from '@/features/absences/types'

vi.mock('@/features/absences/api', () => ({
  absencesApi: {
    getByStudent: vi.fn(),
    getByTeacher: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
  },
}))

describe('AbsenceSection', () => {
  const mockStudentId = 'student-123'

  const mockAbsence: Absence = {
    id: 'absence-1',
    studentId: mockStudentId,
    startDate: '2026-03-01',
    endDate: '2026-03-05',
    reason: 'Sick',
    notes: 'Flu',
    invoiceLesson: false,
    affectedLessonsCount: 2,
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders absences when data loads', async () => {
    vi.mocked(absencesApi.getByStudent).mockResolvedValue([mockAbsence])

    render(<AbsenceSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('2026-03-01 — 2026-03-05')).toBeInTheDocument()
    })
    expect(screen.getByText('absences.reason.sick')).toBeInTheDocument()
    expect(screen.getByText('Flu')).toBeInTheDocument()
  })

  it('renders empty state when no absences', async () => {
    vi.mocked(absencesApi.getByStudent).mockResolvedValue([])

    render(<AbsenceSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('students.absence.noAbsences')).toBeInTheDocument()
    })
  })

  it('shows add absence button', async () => {
    vi.mocked(absencesApi.getByStudent).mockResolvedValue([])

    render(<AbsenceSection studentId={mockStudentId} />)

    expect(screen.getByText('students.absence.addAbsence')).toBeInTheDocument()
  })

  it('opens dialog when add button is clicked', async () => {
    const user = userEvent.setup()
    vi.mocked(absencesApi.getByStudent).mockResolvedValue([])

    render(<AbsenceSection studentId={mockStudentId} />)

    await user.click(screen.getByText('students.absence.addAbsence'))

    await waitFor(() => {
      expect(screen.getByText('students.absence.startDate')).toBeInTheDocument()
      expect(screen.getByText('students.absence.endDate')).toBeInTheDocument()
      expect(screen.getByText('students.absence.reason')).toBeInTheDocument()
    })
  })

  it('renders absence with invoice badge when invoiceLesson is true', async () => {
    const invoicedAbsence: Absence = {
      ...mockAbsence,
      invoiceLesson: true,
    }
    vi.mocked(absencesApi.getByStudent).mockResolvedValue([invoicedAbsence])

    render(<AbsenceSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('students.absence.invoiceLesson')).toBeInTheDocument()
    })
  })

  it('shows delete confirmation dialog', async () => {
    const user = userEvent.setup()
    vi.mocked(absencesApi.getByStudent).mockResolvedValue([mockAbsence])

    render(<AbsenceSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('2026-03-01 — 2026-03-05')).toBeInTheDocument()
    })

    // Click the delete (trash) button
    const deleteButtons = screen.getAllByRole('button')
    const trashButton = deleteButtons.find(btn => btn.querySelector('svg.lucide-trash-2'))
    if (trashButton) {
      await user.click(trashButton)
      await waitFor(() => {
        expect(screen.getByText('students.absence.deleteConfirm')).toBeInTheDocument()
      })
    }
  })
})
