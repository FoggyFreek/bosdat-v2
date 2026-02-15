import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { RecordPaymentDialog } from '../RecordPaymentDialog'
import { invoicesApi } from '@/features/students/api'
import type { InvoicePayment } from '@/features/students/types'

vi.mock('@/features/students/api', () => ({
  invoicesApi: {
    recordPayment: vi.fn(),
  },
}))

describe('RecordPaymentDialog', () => {
  const defaultProps = {
    open: true,
    onOpenChange: vi.fn(),
    invoiceId: 'invoice-1',
    invoiceNumber: '202601',
    remainingBalance: 121,
    studentId: 'student-123',
  }

  const mockPaymentResponse: InvoicePayment = {
    id: 'payment-1',
    invoiceId: 'invoice-1',
    amount: 100,
    paymentDate: '2026-01-20',
    method: 'Bank',
    reference: 'REF-001',
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(invoicesApi.recordPayment).mockResolvedValue(mockPaymentResponse)
  })

  it('renders dialog with title and description', () => {
    render(<RecordPaymentDialog {...defaultProps} />)

    // Title and submit button both show recordPayment text
    expect(screen.getAllByText('students.payments.recordPayment').length).toBeGreaterThanOrEqual(2)
    expect(screen.getByText('students.payments.recordPaymentDesc')).toBeInTheDocument()
  })

  it('renders all form fields', () => {
    render(<RecordPaymentDialog {...defaultProps} />)

    expect(screen.getByText('students.payments.amount')).toBeInTheDocument()
    expect(screen.getByText('students.payments.paymentDate')).toBeInTheDocument()
    expect(screen.getByText('students.payments.method')).toBeInTheDocument()
    expect(screen.getByText('students.payments.reference')).toBeInTheDocument()
    expect(screen.getByText('students.payments.notes')).toBeInTheDocument()
  })

  it('renders cancel and submit buttons', () => {
    render(<RecordPaymentDialog {...defaultProps} />)

    expect(screen.getByText('common.actions.cancel')).toBeInTheDocument()
    // Submit button shows recordPayment text
    const submitButtons = screen.getAllByText('students.payments.recordPayment')
    expect(submitButtons.length).toBeGreaterThanOrEqual(2) // title + button
  })

  it('disables submit when amount is empty', () => {
    render(<RecordPaymentDialog {...defaultProps} />)

    const submitButtons = screen.getAllByRole('button')
    const submitButton = submitButtons.find(
      (btn) => btn.textContent === 'students.payments.recordPayment' && !btn.closest('[data-dialog-header]'),
    )
    // The submit button should be disabled when amount is empty
    expect(submitButton).toBeDisabled()
  })

  it('shows error when amount exceeds remaining balance', async () => {
    const user = userEvent.setup()
    render(<RecordPaymentDialog {...defaultProps} />)

    const amountInput = screen.getByLabelText('students.payments.amount')
    await user.type(amountInput, '200')

    await waitFor(() => {
      expect(screen.getByText('students.payments.exceedsBalance')).toBeInTheDocument()
    })
  })

  it('shows error for invalid amount', async () => {
    const user = userEvent.setup()
    render(<RecordPaymentDialog {...defaultProps} />)

    const amountInput = screen.getByLabelText('students.payments.amount')
    await user.type(amountInput, '0')

    await waitFor(() => {
      expect(screen.getByText('students.payments.invalidAmount')).toBeInTheDocument()
    })
  })

  it('enables submit when valid amount is entered', async () => {
    const user = userEvent.setup()
    render(<RecordPaymentDialog {...defaultProps} />)

    const amountInput = screen.getByLabelText('students.payments.amount')
    await user.type(amountInput, '100')

    await waitFor(() => {
      const buttons = screen.getAllByRole('button')
      const submitButton = buttons.find(
        (btn) => btn.textContent === 'students.payments.recordPayment',
      )
      expect(submitButton).not.toBeDisabled()
    })
  })

  it('calls recordPayment API on submit with correct data', async () => {
    const user = userEvent.setup()
    render(<RecordPaymentDialog {...defaultProps} />)

    // Enter amount
    const amountInput = screen.getByLabelText('students.payments.amount')
    await user.type(amountInput, '100')

    // Enter reference
    const referenceInput = screen.getByLabelText('students.payments.reference')
    await user.type(referenceInput, 'REF-001')

    // Enter notes
    const notesInput = screen.getByLabelText('students.payments.notes')
    await user.type(notesInput, 'Test payment')

    // Click submit
    const buttons = screen.getAllByRole('button')
    const submitButton = buttons.find(
      (btn) => btn.textContent === 'students.payments.recordPayment',
    )
    await user.click(submitButton!)

    await waitFor(() => {
      expect(invoicesApi.recordPayment).toHaveBeenCalledWith('invoice-1', {
        amount: 100,
        paymentDate: expect.any(String),
        method: 'Bank',
        reference: 'REF-001',
        notes: 'Test payment',
      })
    })
  })

  it('calls onOpenChange(false) after successful submit', async () => {
    const user = userEvent.setup()
    render(<RecordPaymentDialog {...defaultProps} />)

    const amountInput = screen.getByLabelText('students.payments.amount')
    await user.type(amountInput, '100')

    const buttons = screen.getAllByRole('button')
    const submitButton = buttons.find(
      (btn) => btn.textContent === 'students.payments.recordPayment',
    )
    await user.click(submitButton!)

    await waitFor(() => {
      expect(defaultProps.onOpenChange).toHaveBeenCalledWith(false)
    })
  })

  it('calls onOpenChange when cancel is clicked', async () => {
    const user = userEvent.setup()
    render(<RecordPaymentDialog {...defaultProps} />)

    await user.click(screen.getByText('common.actions.cancel'))

    expect(defaultProps.onOpenChange).toHaveBeenCalledWith(false)
  })

  it('accepts amount equal to remaining balance', async () => {
    const user = userEvent.setup()
    render(<RecordPaymentDialog {...defaultProps} />)

    const amountInput = screen.getByLabelText('students.payments.amount')
    await user.type(amountInput, '121')

    await waitFor(() => {
      expect(screen.queryByText('students.payments.exceedsBalance')).not.toBeInTheDocument()
      expect(screen.queryByText('students.payments.invalidAmount')).not.toBeInTheDocument()
    })
  })

  it('does not render when open is false', () => {
    render(<RecordPaymentDialog {...defaultProps} open={false} />)

    expect(screen.queryByText('students.payments.recordPayment')).not.toBeInTheDocument()
  })

  it('renders payment date input with default value', () => {
    render(<RecordPaymentDialog {...defaultProps} />)

    const dateInput = screen.getByLabelText('students.payments.paymentDate')
    expect(dateInput).toHaveValue(new Date().toISOString().split('T')[0])
  })
})
