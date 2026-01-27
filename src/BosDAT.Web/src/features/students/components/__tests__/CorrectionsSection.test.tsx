import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { CorrectionsSection } from '../CorrectionsSection'
import { studentLedgerApi, enrollmentsApi } from '@/services/api'
import type {
  StudentLedgerEntry,
  StudentEnrollment,
  EnrollmentPricing,
} from '@/features/students/types'

vi.mock('@/services/api', () => ({
  studentLedgerApi: {
    getByStudent: vi.fn(),
    create: vi.fn(),
    reverse: vi.fn(),
  },
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
    dayOfWeek: 1,
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
    await user.click(screen.getByRole('button', { name: /^all$/i }))

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
      expect(screen.getByText('Ledger Entries')).toBeInTheDocument()
    })

    // Open add form
    await user.click(screen.getByRole('button', { name: /add correction/i }))

    // Manual Entry should be visible
    expect(screen.getByRole('button', { name: /manual entry/i })).toBeInTheDocument()

    // Switch to Course-Based
    await user.click(screen.getByRole('button', { name: /course-based/i }))

    // Course enrollment label should be visible
    await waitFor(() => {
      expect(screen.getByText(/course enrollment/i)).toBeInTheDocument()
      expect(screen.getByText(/number of lessons/i)).toBeInTheDocument()
    })

    // Switch back to Manual
    await user.click(screen.getByRole('button', { name: /manual entry/i }))

    // Amount field should be visible again
    await waitFor(() => {
      expect(screen.getByLabelText(/amount/i)).toBeInTheDocument()
    })
  })

  it('shows "No active corrections" when filtered list is empty', async () => {
    vi.mocked(studentLedgerApi.getByStudent).mockResolvedValue([
      mockAppliedEntry,
      mockReversedEntry,
    ])

    render(<CorrectionsSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText(/no active corrections/i)).toBeInTheDocument()
    })
  })

  it('shows "No corrections yet" when all list is empty', async () => {
    const user = userEvent.setup()
    vi.mocked(studentLedgerApi.getByStudent).mockResolvedValue([])

    render(<CorrectionsSection studentId={mockStudentId} />)

    // Switch to "All" filter to see the empty message
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /^all$/i })).toBeInTheDocument()
    })
    await user.click(screen.getByRole('button', { name: /^all$/i }))

    await waitFor(() => {
      expect(screen.getByText(/no corrections yet/i)).toBeInTheDocument()
    })
  })

  it('disables submit button when form is invalid', async () => {
    const user = userEvent.setup()
    render(<CorrectionsSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('Ledger Entries')).toBeInTheDocument()
    })

    // Open add form
    await user.click(screen.getByRole('button', { name: /add correction/i }))

    // Submit button should be disabled initially
    expect(screen.getByRole('button', { name: /add entry/i })).toBeDisabled()

    // Fill in description only
    await user.type(screen.getByLabelText(/description/i), 'Test')

    // Still disabled (no amount)
    expect(screen.getByRole('button', { name: /add entry/i })).toBeDisabled()

    // Add amount
    await user.type(screen.getByLabelText(/amount/i), '50')

    // Now enabled
    expect(screen.getByRole('button', { name: /add entry/i })).not.toBeDisabled()
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
      expect(screen.getByText('Ledger Entries')).toBeInTheDocument()
    })

    // Open add form
    await user.click(screen.getByRole('button', { name: /add correction/i }))

    // Fill in some data
    await user.type(screen.getByLabelText(/description/i), 'Test description')
    await user.type(screen.getByLabelText(/amount/i), '100')

    // Cancel
    await user.click(screen.getByRole('button', { name: /cancel/i }))

    // Form should be closed
    expect(screen.queryByLabelText(/description/i)).not.toBeInTheDocument()

    // Re-open form
    await user.click(screen.getByRole('button', { name: /add correction/i }))

    // Fields should be empty (reset)
    expect(screen.getByLabelText(/description/i)).toHaveValue('')
    expect(screen.getByLabelText(/amount/i)).toHaveValue(null)
  })

  it('displays entry type badges correctly', async () => {
    render(<CorrectionsSection studentId={mockStudentId} />)

    await waitFor(() => {
      // Check that Credit and Debit badges appear for the visible entries
      const creditBadges = screen.getAllByText('Credit')
      expect(creditBadges.length).toBeGreaterThan(0)
    })
  })

  it('displays status badges correctly', async () => {
    render(<CorrectionsSection studentId={mockStudentId} />)

    await waitFor(() => {
      // Open and PartiallyApplied should be visible
      expect(screen.getByText('Open')).toBeInTheDocument()
      expect(screen.getByText('PartiallyApplied')).toBeInTheDocument()
    })
  })
})
