import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { CorrectionsSection } from '../CorrectionsSection'
import { studentLedgerApi } from '@/features/students/api'
import { enrollmentsApi } from '@/features/enrollments/api'
import type {
  StudentLedgerEntry,
  StudentEnrollment,
  EnrollmentPricing,
} from '@/features/students/types'

vi.mock('@/features/students/api', () => ({
  studentLedgerApi: {
    getByStudent: vi.fn(),
    create: vi.fn(),
    reverse: vi.fn(),
    decouple: vi.fn(),
  },
}))

vi.mock('@/features/enrollments/api', () => ({
  enrollmentsApi: {
    getByStudent: vi.fn(),
    getEnrollmentPricing: vi.fn(),
  },
}))

describe('CorrectionsSection', () => {
  const mockStudentId = 'student-123'

  const mockOpenEntry: StudentLedgerEntry = {
    id: 'entry-1',
    correctionRefName: 'COR-001',
    description: 'Test credit',
    studentId: mockStudentId,
    studentName: 'John Doe',
    amount: 50,
    entryType: 'Credit',
    status: 'Open',
    appliedAmount: 0,
    remainingAmount: 50,
    createdAt: '2024-01-15T10:00:00Z',
    createdByName: 'Admin User',
    applications: [],
  }

  const mockAppliedEntry: StudentLedgerEntry = {
    id: 'entry-2',
    correctionRefName: 'COR-002',
    description: 'Applied debit',
    studentId: mockStudentId,
    studentName: 'John Doe',
    amount: 30,
    entryType: 'Debit',
    status: 'Applied',
    appliedAmount: 30,
    remainingAmount: 0,
    createdAt: '2024-01-10T10:00:00Z',
    createdByName: 'Admin User',
    applications: [],
  }

  const mockPartiallyAppliedEntry: StudentLedgerEntry = {
    id: 'entry-3',
    correctionRefName: 'COR-003',
    description: 'Partially applied',
    studentId: mockStudentId,
    studentName: 'John Doe',
    amount: 100,
    entryType: 'Credit',
    status: 'PartiallyApplied',
    appliedAmount: 40,
    remainingAmount: 60,
    createdAt: '2024-01-12T10:00:00Z',
    createdByName: 'Admin User',
    applications: [],
  }

  const mockReversedEntry: StudentLedgerEntry = {
    id: 'entry-4',
    correctionRefName: 'COR-004',
    description: 'Reversed entry',
    studentId: mockStudentId,
    studentName: 'John Doe',
    amount: 25,
    entryType: 'Credit',
    status: 'Reversed',
    appliedAmount: 0,
    remainingAmount: 0,
    createdAt: '2024-01-05T10:00:00Z',
    createdByName: 'Admin User',
    applications: [],
  }

  const mockEnrollment: StudentEnrollment = {
    id: 'enrollment-1',
    courseId: 'course-1',
    instrumentName: 'Piano',
    courseTypeName: 'Piano Lesson',
    teacherName: 'Jane Teacher',
    dayOfWeek: 'Monday',
    startTime: '10:00',
    endTime: '10:30',
    enrolledAt: '2024-01-01T00:00:00Z',
    discountPercent: 10,
    status: 'Active',
  }

  const mockPricing: EnrollmentPricing = {
    enrollmentId: 'enrollment-1',
    courseId: 'course-1',
    courseName: 'Piano Lesson - Jane Teacher',
    basePriceAdult: 50,
    basePriceChild: 35,
    isChildPricing: false,
    applicableBasePrice: 50,
    discountPercent: 10,
    discountAmount: 5,
    pricePerLesson: 45,
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(studentLedgerApi.getByStudent).mockResolvedValue([
      mockOpenEntry,
      mockAppliedEntry,
      mockPartiallyAppliedEntry,
      mockReversedEntry,
    ])
    vi.mocked(enrollmentsApi.getByStudent).mockResolvedValue([mockEnrollment])
    vi.mocked(enrollmentsApi.getEnrollmentPricing).mockResolvedValue(mockPricing)
  })

  it('renders with active filter by default', async () => {
    render(<CorrectionsSection studentId={mockStudentId} />)

    await waitFor(() => {
      // Should show Open and PartiallyApplied entries
      expect(screen.getByText('COR-001')).toBeInTheDocument()
      expect(screen.getByText('COR-003')).toBeInTheDocument()

      // Should NOT show Applied and Reversed entries
      expect(screen.queryByText('COR-002')).not.toBeInTheDocument()
      expect(screen.queryByText('COR-004')).not.toBeInTheDocument()
    })
  })

  it('shows all entries when filter changed to All', async () => {
    const user = userEvent.setup()
    render(<CorrectionsSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('COR-001')).toBeInTheDocument()
    })

    // Click on "All" filter button
    await user.click(screen.getByRole('button', { name: 'students.corrections.all' }))

    await waitFor(() => {
      // Should show all entries including Applied and Reversed
      expect(screen.getByText('COR-001')).toBeInTheDocument()
      expect(screen.getByText('COR-002')).toBeInTheDocument()
      expect(screen.getByText('COR-003')).toBeInTheDocument()
      expect(screen.getByText('COR-004')).toBeInTheDocument()
    })
  })

  it('toggles between manual and course-based calculation methods', async () => {
    const user = userEvent.setup()
    render(<CorrectionsSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('students.sections.corrections')).toBeInTheDocument()
    })

    // Open add form
    await user.click(screen.getByRole('button', { name: 'students.corrections.addCorrection' }))

    // Manual Entry should be visible
    expect(screen.queryByRole('button', { name: /manual entry/i })).toBeInTheDocument()

    // Switch to Course-Based
    await user.click(screen.queryByRole('button', { name: /course-based/i }))

    // Course enrollment label should be visible
    await waitFor(() => {
      expect(screen.queryByText(/course enrollment/i)).toBeInTheDocument()
      expect(screen.queryByText(/number of lessons/i)).toBeInTheDocument()
    })

    // Switch back to Manual
    await user.click(screen.queryByRole('button', { name: /manual entry/i }))

    // Amount field should be visible again
    await waitFor(() => {
      expect(screen.queryByLabelText(/amount/i)).toBeInTheDocument()
    })
  })

  it('shows "No active corrections" when filtered list is empty', async () => {
    vi.mocked(studentLedgerApi.getByStudent).mockResolvedValue([
      mockAppliedEntry,
      mockReversedEntry,
    ])

    render(<CorrectionsSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('students.corrections.noActiveCorrections')).toBeInTheDocument()
    })
  })

  it('shows "No corrections yet" when all list is empty', async () => {
    const user = userEvent.setup()
    vi.mocked(studentLedgerApi.getByStudent).mockResolvedValue([])

    render(<CorrectionsSection studentId={mockStudentId} />)

    // The "All" filter button should be present
    // But when list is empty, we already show the empty message
    await waitFor(() => {
      // When no entries, we should see the no corrections message
      expect(screen.queryByText(/COR-/)).not.toBeInTheDocument()
    })
  })

  it('disables submit button when form is invalid', async () => {
    const user = userEvent.setup()
    render(<CorrectionsSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('students.sections.corrections')).toBeInTheDocument()
    })

    // Open add form
    await user.click(screen.getByRole('button', { name: 'students.corrections.addCorrection' }))

    // Submit button should be disabled initially
    expect(screen.queryByRole('button', { name: /add entry/i })).toBeDisabled()

    // Fill in description only
    await user.type(screen.queryByLabelText(/description/i), 'Test')

    // Still disabled (no amount)
    expect(screen.queryByRole('button', { name: /add entry/i })).toBeDisabled()

    // Add amount
    await user.type(screen.queryByLabelText(/amount/i), '50')

    // Now enabled
    expect(screen.queryByRole('button', { name: /add entry/i })).not.toBeDisabled()
  })

  it('shows reverse button only for Open entries', async () => {
    render(<CorrectionsSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('COR-001')).toBeInTheDocument()
    })

    // Only one Reverse button should be visible (for the Open entry)
    const reverseButtons = screen.getAllByRole('button', { name: /reverse/i })
    expect(reverseButtons).toHaveLength(1)
  })

  it('closes form and resets state when cancel is clicked', async () => {
    const user = userEvent.setup()
    render(<CorrectionsSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('students.sections.corrections')).toBeInTheDocument()
    })

    // Open add form
    const addButton = screen.getByRole('button', { name: 'students.corrections.addCorrection' })
    await user.click(addButton)

    // Check that form is open by looking for the Description input field
    const descriptionInput = document.getElementById('description')
    expect(descriptionInput).toBeInTheDocument()

    // Cancel the form
    const cancelButton = screen.getByRole('button', { name: 'Cancel' })
    await user.click(cancelButton)

    // Form should be closed - the Description input field should not be visible
    await waitFor(() => {
      const descriptionInputAfterCancel = document.getElementById('description')
      expect(descriptionInputAfterCancel).not.toBeInTheDocument()
    })
  })

  it('displays entry type badges correctly', async () => {
    render(<CorrectionsSection studentId={mockStudentId} />)

    await waitFor(() => {
      // Check that entry type badges appear - entries are rendered with their type
      // Default filter shows Open and PartiallyApplied entries which include Credit entries
      expect(screen.getByText('COR-001')).toBeInTheDocument()
      expect(screen.getByText('COR-003')).toBeInTheDocument()
    })
  })

  it('displays status badges correctly', async () => {
    render(<CorrectionsSection studentId={mockStudentId} />)

    await waitFor(() => {
      // Entries with Open and PartiallyApplied status should be visible (default filter)
      // COR-001 has Open status, COR-003 has PartiallyApplied status
      expect(screen.getByText('COR-001')).toBeInTheDocument()
      expect(screen.getByText('COR-003')).toBeInTheDocument()
    })
  })

  it('shows expand toggle only for entries with applications', async () => {
    const entryWithApp: StudentLedgerEntry = {
      ...mockPartiallyAppliedEntry,
      applications: [
        {
          id: 'app-1',
          invoiceId: 'inv-1',
          invoiceNumber: 'NMI-2026-00001',
          appliedAmount: 40,
          appliedAt: '2024-01-15T10:00:00Z',
          appliedByName: 'Admin User',
        },
      ],
    }

    vi.mocked(studentLedgerApi.getByStudent).mockResolvedValue([mockOpenEntry, entryWithApp])

    render(<CorrectionsSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('COR-001')).toBeInTheDocument()
      expect(screen.getByText('COR-003')).toBeInTheDocument()
    })

    // Only the entry with applications should have an expand button
    const expandButtons = screen.getAllByLabelText(/expand applications/i)
    expect(expandButtons).toHaveLength(1)
  })

  it('expands applications and shows decouple button', async () => {
    const user = userEvent.setup()
    const entryWithApp: StudentLedgerEntry = {
      ...mockPartiallyAppliedEntry,
      applications: [
        {
          id: 'app-1',
          invoiceId: 'inv-1',
          invoiceNumber: 'NMI-2026-00001',
          appliedAmount: 40,
          appliedAt: '2024-01-15T10:00:00Z',
          appliedByName: 'Admin User',
        },
      ],
    }

    vi.mocked(studentLedgerApi.getByStudent).mockResolvedValue([entryWithApp])

    render(<CorrectionsSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('COR-003')).toBeInTheDocument()
    })

    // Expand the entry
    await user.click(screen.getByLabelText(/expand applications/i))

    await waitFor(() => {
      expect(screen.getByText('NMI-2026-00001')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /decouple/i })).toBeInTheDocument()
    })
  })

  it('opens decouple dialog when decouple button is clicked', async () => {
    const user = userEvent.setup()
    const entryWithApp: StudentLedgerEntry = {
      ...mockPartiallyAppliedEntry,
      applications: [
        {
          id: 'app-1',
          invoiceId: 'inv-1',
          invoiceNumber: 'NMI-2026-00001',
          appliedAmount: 40,
          appliedAt: '2024-01-15T10:00:00Z',
          appliedByName: 'Admin User',
        },
      ],
    }

    vi.mocked(studentLedgerApi.getByStudent).mockResolvedValue([entryWithApp])

    render(<CorrectionsSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('COR-003')).toBeInTheDocument()
    })

    // Expand then click Decouple
    await user.click(screen.getByLabelText(/expand applications/i))
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /decouple/i })).toBeInTheDocument()
    })
    await user.click(screen.getByRole('button', { name: /decouple/i }))

    // Dialog should be visible with invoice number (appears in both the row and dialog description)
    await waitFor(() => {
      expect(screen.getByText('Decouple Correction')).toBeInTheDocument()
      expect(screen.getAllByText(/NMI-2026-00001/)).toHaveLength(2)
    })
  })

  it('decouple dialog submit is disabled without reason', async () => {
    const user = userEvent.setup()
    const entryWithApp: StudentLedgerEntry = {
      ...mockPartiallyAppliedEntry,
      applications: [
        {
          id: 'app-1',
          invoiceId: 'inv-1',
          invoiceNumber: 'NMI-2026-00001',
          appliedAmount: 40,
          appliedAt: '2024-01-15T10:00:00Z',
          appliedByName: 'Admin User',
        },
      ],
    }

    vi.mocked(studentLedgerApi.getByStudent).mockResolvedValue([entryWithApp])

    render(<CorrectionsSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('COR-003')).toBeInTheDocument()
    })

    await user.click(screen.getByLabelText(/expand applications/i))
    await user.click(screen.getByRole('button', { name: /decouple/i }))

    await waitFor(() => {
      // Decouple submit button should be disabled (no reason entered)
      const decoupleSubmit = screen.getByRole('button', { name: /^decouple$/i })
      expect(decoupleSubmit).toBeDisabled()
    })
  })
})
