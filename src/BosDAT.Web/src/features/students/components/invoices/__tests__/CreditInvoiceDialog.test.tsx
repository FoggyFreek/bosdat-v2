import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { CreditInvoiceDialog } from '../CreditInvoiceDialog'
import { invoicesApi } from '@/features/students/api'
import type { Invoice } from '@/features/students/types'

vi.mock('@/features/students/api', () => ({
  invoicesApi: {
    createCreditInvoice: vi.fn(),
    getByStudent: vi.fn(),
    getById: vi.fn(),
  },
}))

describe('CreditInvoiceDialog', () => {
  const mockStudentId = 'student-123'
  const mockOnOpenChange = vi.fn()

  const mockInvoice: Invoice = {
    id: 'invoice-1',
    invoiceNumber: '202601',
    studentId: mockStudentId,
    enrollmentId: 'enrollment-1',
    studentName: 'John Doe',
    studentEmail: 'john@example.com',
    issueDate: '2026-01-15',
    dueDate: '2026-01-29',
    description: 'Piano Individual 30min jan26',
    periodStart: '2026-01-01',
    periodEnd: '2026-01-31',
    periodType: 'Monthly',
    subtotal: 100,
    vatAmount: 21,
    total: 121,
    discountAmount: 0,
    status: 'Sent',
    amountPaid: 0,
    balance: 121,
    createdAt: '2026-01-15T10:00:00Z',
    updatedAt: '2026-01-15T10:00:00Z',
    isCreditInvoice: false,
    lines: [
      {
        id: 1,
        description: 'Piano - 06 Jan 2026',
        quantity: 1,
        unitPrice: 25,
        vatRate: 21,
        lineTotal: 25,
      },
      {
        id: 2,
        description: 'Piano - 13 Jan 2026',
        quantity: 1,
        unitPrice: 25,
        vatRate: 21,
        lineTotal: 25,
      },
      {
        id: 3,
        description: 'Piano - 20 Jan 2026',
        quantity: 1,
        unitPrice: 25,
        vatRate: 21,
        lineTotal: 25,
      },
    ],
    payments: [],
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders dialog with invoice lines', () => {
    render(
      <CreditInvoiceDialog
        open={true}
        onOpenChange={mockOnOpenChange}
        invoice={mockInvoice}
        studentId={mockStudentId}
      />,
    )

    expect(screen.getByText('students.creditInvoice.title')).toBeInTheDocument()
    expect(screen.getByText('Piano - 06 Jan 2026')).toBeInTheDocument()
    expect(screen.getByText('Piano - 13 Jan 2026')).toBeInTheDocument()
    expect(screen.getByText('Piano - 20 Jan 2026')).toBeInTheDocument()
  })

  it('disables create button when no lines selected', () => {
    render(
      <CreditInvoiceDialog
        open={true}
        onOpenChange={mockOnOpenChange}
        invoice={mockInvoice}
        studentId={mockStudentId}
      />,
    )

    const createButton = screen.getByRole('button', { name: 'students.creditInvoice.create' })
    expect(createButton).toBeDisabled()
  })

  it('enables create button when a line is selected', async () => {
    const user = userEvent.setup()
    render(
      <CreditInvoiceDialog
        open={true}
        onOpenChange={mockOnOpenChange}
        invoice={mockInvoice}
        studentId={mockStudentId}
      />,
    )

    // Click on a line to select it
    await user.click(screen.getByText('Piano - 06 Jan 2026'))

    const createButton = screen.getByRole('button', { name: 'students.creditInvoice.create' })
    expect(createButton).not.toBeDisabled()
  })

  it('shows credit total when lines are selected', async () => {
    const user = userEvent.setup()
    render(
      <CreditInvoiceDialog
        open={true}
        onOpenChange={mockOnOpenChange}
        invoice={mockInvoice}
        studentId={mockStudentId}
      />,
    )

    await user.click(screen.getByText('Piano - 06 Jan 2026'))

    await waitFor(() => {
      expect(screen.getByText('students.creditInvoice.creditTotal')).toBeInTheDocument()
    })
  })

  it('selects all lines when select all checkbox clicked', async () => {
    const user = userEvent.setup()
    render(
      <CreditInvoiceDialog
        open={true}
        onOpenChange={mockOnOpenChange}
        invoice={mockInvoice}
        studentId={mockStudentId}
      />,
    )

    const selectAllLabel = screen.getByText('students.creditInvoice.selectAll')
    await user.click(selectAllLabel)

    const createButton = screen.getByRole('button', { name: 'students.creditInvoice.create' })
    expect(createButton).not.toBeDisabled()
  })

  it('calls createCreditInvoice API when submitted', async () => {
    const creditInvoiceResult: Invoice = {
      ...mockInvoice,
      id: 'credit-1',
      invoiceNumber: 'C-202601',
      isCreditInvoice: true,
      originalInvoiceId: 'invoice-1',
      originalInvoiceNumber: '202601',
      status: 'Draft',
    }
    vi.mocked(invoicesApi.createCreditInvoice).mockResolvedValue(creditInvoiceResult)

    const user = userEvent.setup()
    render(
      <CreditInvoiceDialog
        open={true}
        onOpenChange={mockOnOpenChange}
        invoice={mockInvoice}
        studentId={mockStudentId}
      />,
    )

    // Select first two lines
    await user.click(screen.getByText('Piano - 06 Jan 2026'))
    await user.click(screen.getByText('Piano - 13 Jan 2026'))

    // Submit
    await user.click(screen.getByRole('button', { name: 'students.creditInvoice.create' }))

    await waitFor(() => {
      expect(invoicesApi.createCreditInvoice).toHaveBeenCalledWith('invoice-1', {
        selectedLineIds: [1, 2],
        notes: undefined,
      })
    })
  })

  it('shows draft notice text', () => {
    render(
      <CreditInvoiceDialog
        open={true}
        onOpenChange={mockOnOpenChange}
        invoice={mockInvoice}
        studentId={mockStudentId}
      />,
    )

    expect(screen.getByText('students.creditInvoice.draftNotice')).toBeInTheDocument()
  })

  it('calls onOpenChange when cancel clicked', async () => {
    const user = userEvent.setup()
    render(
      <CreditInvoiceDialog
        open={true}
        onOpenChange={mockOnOpenChange}
        invoice={mockInvoice}
        studentId={mockStudentId}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'common.actions.cancel' }))

    expect(mockOnOpenChange).toHaveBeenCalledWith(false)
  })
})
