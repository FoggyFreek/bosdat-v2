import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { ApplyCreditDialog } from '../ApplyCreditDialog'
import { invoicesApi } from '@/features/students/api'

vi.mock('@/features/students/api', () => ({
  invoicesApi: {
    getAvailableCredit: vi.fn(),
    applyCreditInvoices: vi.fn(),
    getByStudent: vi.fn(),
    getById: vi.fn(),
  },
}))

describe('ApplyCreditDialog', () => {
  const mockStudentId = 'student-123'
  const mockInvoiceId = 'invoice-1'
  const mockOnOpenChange = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(invoicesApi.getAvailableCredit).mockResolvedValue(50)
  })

  it('renders dialog with title and description', async () => {
    render(
      <ApplyCreditDialog
        open={true}
        onOpenChange={mockOnOpenChange}
        invoiceId={mockInvoiceId}
        studentId={mockStudentId}
        remainingBalance={121}
      />,
    )

    expect(screen.getByText('students.creditBalance.title')).toBeInTheDocument()
  })

  it('fetches and displays available credit', async () => {
    render(
      <ApplyCreditDialog
        open={true}
        onOpenChange={mockOnOpenChange}
        invoiceId={mockInvoiceId}
        studentId={mockStudentId}
        remainingBalance={121}
      />,
    )

    await waitFor(() => {
      expect(invoicesApi.getAvailableCredit).toHaveBeenCalledWith(mockStudentId)
    })

    expect(screen.getByText('students.creditBalance.availableCredit')).toBeInTheDocument()
    expect(screen.getByText('students.creditBalance.invoiceBalance')).toBeInTheDocument()
  })

  it('disables apply button when available credit is zero', async () => {
    vi.mocked(invoicesApi.getAvailableCredit).mockResolvedValue(0)

    render(
      <ApplyCreditDialog
        open={true}
        onOpenChange={mockOnOpenChange}
        invoiceId={mockInvoiceId}
        studentId={mockStudentId}
        remainingBalance={121}
      />,
    )

    await waitFor(() => {
      const applyButton = screen.getByRole('button', { name: 'students.creditBalance.apply' })
      expect(applyButton).toBeDisabled()
    })
  })

  it('enables apply button when available credit exists', async () => {
    vi.mocked(invoicesApi.getAvailableCredit).mockResolvedValue(50)

    render(
      <ApplyCreditDialog
        open={true}
        onOpenChange={mockOnOpenChange}
        invoiceId={mockInvoiceId}
        studentId={mockStudentId}
        remainingBalance={121}
      />,
    )

    await waitFor(() => {
      const applyButton = screen.getByRole('button', { name: 'students.creditBalance.apply' })
      expect(applyButton).not.toBeDisabled()
    })
  })

  it('calls applyCreditInvoices when apply button clicked', async () => {
    vi.mocked(invoicesApi.applyCreditInvoices).mockResolvedValue({
      id: mockInvoiceId,
      invoiceNumber: '202601',
      studentId: mockStudentId,
      studentName: 'John Doe',
      studentEmail: 'john@example.com',
      issueDate: '2026-01-15',
      dueDate: '2026-01-29',
      subtotal: 100,
      vatAmount: 21,
      total: 121,
      discountAmount: 0,
      status: 'Paid',
      amountPaid: 121,
      balance: 0,
      createdAt: '2026-01-15T10:00:00Z',
      updatedAt: '2026-01-15T10:00:00Z',
      isCreditInvoice: false,
      lines: [],
      payments: [],
    })

    const user = userEvent.setup()
    render(
      <ApplyCreditDialog
        open={true}
        onOpenChange={mockOnOpenChange}
        invoiceId={mockInvoiceId}
        studentId={mockStudentId}
        remainingBalance={121}
      />,
    )

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'students.creditBalance.apply' })).not.toBeDisabled()
    })

    await user.click(screen.getByRole('button', { name: 'students.creditBalance.apply' }))

    await waitFor(() => {
      expect(invoicesApi.applyCreditInvoices).toHaveBeenCalledWith(mockInvoiceId)
    })
  })

  it('calls onOpenChange when cancel clicked', async () => {
    const user = userEvent.setup()
    render(
      <ApplyCreditDialog
        open={true}
        onOpenChange={mockOnOpenChange}
        invoiceId={mockInvoiceId}
        studentId={mockStudentId}
        remainingBalance={121}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'common.actions.cancel' }))

    expect(mockOnOpenChange).toHaveBeenCalledWith(false)
  })

  it('does not fetch credit when dialog is closed', () => {
    render(
      <ApplyCreditDialog
        open={false}
        onOpenChange={mockOnOpenChange}
        invoiceId={mockInvoiceId}
        studentId={mockStudentId}
        remainingBalance={121}
      />,
    )

    expect(invoicesApi.getAvailableCredit).not.toHaveBeenCalled()
  })
})
