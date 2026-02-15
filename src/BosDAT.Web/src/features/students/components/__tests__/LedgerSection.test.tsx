import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor, within } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { LedgerSection } from '../LedgerSection'
import { studentTransactionsApi } from '@/features/students/api'
import type { StudentLedgerView, StudentTransaction } from '@/features/students/types'

vi.mock('@/features/students/api', () => ({
  studentTransactionsApi: {
    getLedger: vi.fn(),
    getBalance: vi.fn(),
  },
}))

describe('LedgerSection', () => {
  const mockStudentId = 'student-123'

  const mockTransactions: StudentTransaction[] = [
    {
      id: 'tx-1',
      studentId: mockStudentId,
      transactionDate: '2026-01-15',
      type: 'InvoiceCharge',
      description: 'Piano Individual 30min jan26',
      referenceNumber: 'INV-202601',
      debit: 121,
      credit: 0,
      runningBalance: 121,
      invoiceId: 'invoice-1',
      createdAt: '2026-01-15T10:00:00Z',
      createdByName: 'Admin User',
    },
    {
      id: 'tx-2',
      studentId: mockStudentId,
      transactionDate: '2026-01-20',
      type: 'Payment',
      description: 'Payment for INV-202601',
      referenceNumber: 'PAY-001',
      debit: 0,
      credit: 100,
      runningBalance: 21,
      paymentId: 'payment-1',
      createdAt: '2026-01-20T14:00:00Z',
      createdByName: 'Admin User',
    },
    {
      id: 'tx-3',
      studentId: mockStudentId,
      transactionDate: '2026-01-25',
      type: 'CreditCorrection',
      description: 'Lesson cancellation refund',
      referenceNumber: 'COR-001',
      debit: 0,
      credit: 21,
      runningBalance: 0,
      ledgerEntryId: 'entry-1',
      createdAt: '2026-01-25T09:00:00Z',
      createdByName: 'Admin User',
    },
  ]

  const mockLedgerView: StudentLedgerView = {
    studentId: mockStudentId,
    studentName: 'John Doe',
    currentBalance: 21,
    totalDebited: 121,
    totalCredited: 100,
    transactions: mockTransactions,
  }

  const mockEmptyLedger: StudentLedgerView = {
    studentId: mockStudentId,
    studentName: 'John Doe',
    currentBalance: 0,
    totalDebited: 0,
    totalCredited: 0,
    transactions: [],
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(studentTransactionsApi.getLedger).mockResolvedValue(mockLedgerView)
  })

  it('renders ledger title', async () => {
    render(<LedgerSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('students.ledger.title')).toBeInTheDocument()
    })
  })

  it('renders summary cards with correct values', async () => {
    render(<LedgerSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('students.ledger.currentBalance')).toBeInTheDocument()
      expect(screen.getByText('students.ledger.totalDebited')).toBeInTheDocument()
      expect(screen.getByText('students.ledger.totalCredited')).toBeInTheDocument()
    })
  })

  it('shows owes label when balance is positive', async () => {
    render(<LedgerSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('students.ledger.owes')).toBeInTheDocument()
    })
  })

  it('shows credit label when balance is negative', async () => {
    vi.mocked(studentTransactionsApi.getLedger).mockResolvedValue({
      ...mockLedgerView,
      currentBalance: -50,
    })

    render(<LedgerSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('students.ledger.credit')).toBeInTheDocument()
    })
  })

  it('shows settled label when balance is zero', async () => {
    vi.mocked(studentTransactionsApi.getLedger).mockResolvedValue(mockEmptyLedger)

    render(<LedgerSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('students.ledger.settled')).toBeInTheDocument()
    })
  })

  it('renders transaction table with headers', async () => {
    render(<LedgerSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('students.ledger.table.date')).toBeInTheDocument()
      expect(screen.getByText('students.ledger.table.description')).toBeInTheDocument()
      expect(screen.getByText('students.ledger.table.reference')).toBeInTheDocument()
      expect(screen.getByText('students.ledger.table.type')).toBeInTheDocument()
      expect(screen.getByText('students.ledger.table.debit')).toBeInTheDocument()
      expect(screen.getByText('students.ledger.table.credit')).toBeInTheDocument()
      expect(screen.getByText('students.ledger.table.balance')).toBeInTheDocument()
    })
  })

  it('renders transactions in the table', async () => {
    render(<LedgerSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('Piano Individual 30min jan26')).toBeInTheDocument()
      expect(screen.getByText('Payment for INV-202601')).toBeInTheDocument()
      expect(screen.getByText('Lesson cancellation refund')).toBeInTheDocument()
    })
  })

  it('renders reference numbers', async () => {
    render(<LedgerSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('INV-202601')).toBeInTheDocument()
      expect(screen.getByText('PAY-001')).toBeInTheDocument()
      expect(screen.getByText('COR-001')).toBeInTheDocument()
    })
  })

  it('renders transaction type badges', async () => {
    render(<LedgerSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('students.ledger.transactionType.invoiceCharge')).toBeInTheDocument()
      expect(screen.getByText('students.ledger.transactionType.payment')).toBeInTheDocument()
      expect(screen.getByText('students.ledger.transactionType.creditCorrection')).toBeInTheDocument()
    })
  })

  it('shows empty state when no transactions exist', async () => {
    vi.mocked(studentTransactionsApi.getLedger).mockResolvedValue(mockEmptyLedger)

    render(<LedgerSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('students.ledger.noTransactions')).toBeInTheDocument()
    })
  })

  it('renders filter dropdown with all type options', async () => {
    render(<LedgerSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('students.ledger.transactions')).toBeInTheDocument()
    })
  })

  it('filters transactions by type when filter is changed', async () => {
    // Radix Select sets pointer-events:none on body when open
    const user = userEvent.setup({ pointerEventsCheck: 0 })
    render(<LedgerSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('Piano Individual 30min jan26')).toBeInTheDocument()
    })

    // Click the filter dropdown trigger
    const trigger = screen.getByRole('combobox')
    await user.click(trigger)

    // Find the Payment option in the dropdown listbox by role
    const listbox = await screen.findByRole('listbox')
    const paymentOption = within(listbox).getByText('students.ledger.transactionType.payment')
    await user.click(paymentOption)

    // Only the payment transaction should be visible
    await waitFor(() => {
      expect(screen.getByText('Payment for INV-202601')).toBeInTheDocument()
      expect(screen.queryByText('Piano Individual 30min jan26')).not.toBeInTheDocument()
      expect(screen.queryByText('Lesson cancellation refund')).not.toBeInTheDocument()
    })
  })

  it('shows loading spinner while data is loading', () => {
    vi.mocked(studentTransactionsApi.getLedger).mockImplementation(
      () => new Promise(() => {}), // never resolves
    )

    render(<LedgerSection studentId={mockStudentId} />)

    // The spinner has the animate-spin class
    const spinner = document.querySelector('.animate-spin')
    expect(spinner).toBeInTheDocument()
  })

  it('calls getLedger API with correct studentId', async () => {
    render(<LedgerSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(studentTransactionsApi.getLedger).toHaveBeenCalledWith(mockStudentId)
    })
  })
})
