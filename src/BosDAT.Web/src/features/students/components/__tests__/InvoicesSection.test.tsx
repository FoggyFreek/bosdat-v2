import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { InvoicesSection } from '../InvoicesSection'
import { invoicesApi } from '@/features/students/api'
import type { Invoice, InvoiceListItem, SchoolBillingInfo } from '@/features/students/types'

vi.mock('@/features/students/api', () => ({
  invoicesApi: {
    getByStudent: vi.fn(),
    getById: vi.fn(),
    recalculate: vi.fn(),
    getSchoolBillingInfo: vi.fn(),
    recordPayment: vi.fn(),
    getPayments: vi.fn(),
    createCreditInvoice: vi.fn(),
    confirmCreditInvoice: vi.fn(),
  },
}))

describe('InvoicesSection', () => {
  const mockStudentId = 'student-123'

  const mockInvoiceListItem: InvoiceListItem = {
    id: 'invoice-1',
    invoiceNumber: '202601',
    studentName: 'John Doe',
    description: 'Piano Individual 30min jan26',
    issueDate: '2026-01-15',
    dueDate: '2026-01-29',
    periodStart: '2026-01-01',
    periodEnd: '2026-01-31',
    total: 121,
    status: 'Draft',
    balance: 121,
    isCreditInvoice: false,
  }

  const mockPaidInvoiceListItem: InvoiceListItem = {
    id: 'invoice-2',
    invoiceNumber: '202602',
    studentName: 'John Doe',
    description: 'Piano Individual 30min feb26',
    issueDate: '2026-02-15',
    dueDate: '2026-02-28',
    periodStart: '2026-02-01',
    periodEnd: '2026-02-28',
    total: 121,
    status: 'Paid',
    balance: 0,
    isCreditInvoice: false,
  }

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
    status: 'Draft',
    amountPaid: 0,
    balance: 121,
    createdAt: '2026-01-15T10:00:00Z',
    updatedAt: '2026-01-15T10:00:00Z',
    isCreditInvoice: false,
    billingContact: {
      name: 'John Doe',
      email: 'john@example.com',
      address: '123 Main St',
      postalCode: '1234AB',
      city: 'Amsterdam',
    },
    lines: [
      {
        id: 1,
        lessonId: 'lesson-1',
        pricingVersionId: 'pricing-1',
        description: 'Piano Individual 30min - 06 Jan 2026',
        quantity: 1,
        unitPrice: 25,
        vatRate: 21,
        lineTotal: 30.25,
        lessonDate: '2026-01-06',
        courseName: 'Piano Individual 30min',
      },
      {
        id: 2,
        lessonId: 'lesson-2',
        pricingVersionId: 'pricing-1',
        description: 'Piano Individual 30min - 13 Jan 2026',
        quantity: 1,
        unitPrice: 25,
        vatRate: 21,
        lineTotal: 30.25,
        lessonDate: '2026-01-13',
        courseName: 'Piano Individual 30min',
      },
      {
        id: 3,
        lessonId: 'lesson-3',
        pricingVersionId: 'pricing-1',
        description: 'Piano Individual 30min - 20 Jan 2026',
        quantity: 1,
        unitPrice: 25,
        vatRate: 21,
        lineTotal: 30.25,
        lessonDate: '2026-01-20',
        courseName: 'Piano Individual 30min',
      },
      {
        id: 4,
        lessonId: 'lesson-4',
        pricingVersionId: 'pricing-1',
        description: 'Piano Individual 30min - 27 Jan 2026',
        quantity: 1,
        unitPrice: 25,
        vatRate: 21,
        lineTotal: 30.25,
        lessonDate: '2026-01-27',
        courseName: 'Piano Individual 30min',
      },
    ],
    payments: [],
  }

  const mockSchoolBillingInfo: SchoolBillingInfo = {
    name: 'Test Music School',
    address: 'Test Street 123',
    postalCode: '1234AB',
    city: 'Test City',
    phone: '0612345678',
    email: 'test@school.nl',
    kvkNumber: '12345678',
    btwNumber: 'NL123456789B01',
    iban: 'NL00TEST0000000001',
    vatRate: 21,
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(invoicesApi.getByStudent).mockResolvedValue([mockInvoiceListItem, mockPaidInvoiceListItem])
    vi.mocked(invoicesApi.getById).mockResolvedValue(mockInvoice)
    vi.mocked(invoicesApi.recalculate).mockResolvedValue(mockInvoice)
    vi.mocked(invoicesApi.getSchoolBillingInfo).mockResolvedValue(mockSchoolBillingInfo)
  })

  it('renders invoice list when invoices exist', async () => {
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('202601')).toBeInTheDocument()
      expect(screen.getByText('202602')).toBeInTheDocument()
    })
  })

  it('shows empty state when no invoices exist', async () => {
    vi.mocked(invoicesApi.getByStudent).mockResolvedValue([])

    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('students.invoices.noInvoices')).toBeInTheDocument()
    })
  })

  it('displays invoice status badges correctly', async () => {
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('students.invoices.status.draft')).toBeInTheDocument()
      expect(screen.getByText('students.invoices.status.paid')).toBeInTheDocument()
    })
  })

  it('expands invoice to show details when clicked', async () => {
    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('202601')).toBeInTheDocument()
    })

    // Click on the invoice row to expand
    const invoiceRow = screen.getByText('202601').closest('button')
    if (invoiceRow) {
      await user.click(invoiceRow)
    }

    await waitFor(() => {
      // Should show invoice line items
      expect(screen.getByText('Piano Individual 30min - 06 Jan 2026')).toBeInTheDocument()
    })
  })

  it('shows recalculate button for draft invoices', async () => {
    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('202601')).toBeInTheDocument()
    })

    // Expand the draft invoice
    const invoiceRow = screen.getByText('202601').closest('button')
    if (invoiceRow) {
      await user.click(invoiceRow)
    }

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'students.invoices.recalculate' })).toBeInTheDocument()
    })
  })

  it('does not show recalculate button for paid invoices', async () => {
    vi.mocked(invoicesApi.getByStudent).mockResolvedValue([mockPaidInvoiceListItem])
    vi.mocked(invoicesApi.getById).mockResolvedValue({
      ...mockInvoice,
      id: 'invoice-2',
      status: 'Paid',
      balance: 0,
    })

    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('202602')).toBeInTheDocument()
    })

    // Expand the paid invoice
    const invoiceRow = screen.getByText('202602').closest('button')
    if (invoiceRow) {
      await user.click(invoiceRow)
    }

    await waitFor(() => {
      expect(screen.queryByRole('button', { name: 'students.invoices.recalculate' })).not.toBeInTheDocument()
    })
  })

  it('calls recalculate mutation when recalculate button clicked', async () => {
    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('202601')).toBeInTheDocument()
    })

    // Expand the invoice
    const invoiceRow = screen.getByText('202601').closest('button')
    if (invoiceRow) {
      await user.click(invoiceRow)
    }

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'students.invoices.recalculate' })).toBeInTheDocument()
    })

    // Click recalculate
    await user.click(screen.getByRole('button', { name: 'students.invoices.recalculate' }))

    await waitFor(() => {
      expect(invoicesApi.recalculate).toHaveBeenCalledWith('invoice-1')
    })
  })

  it('shows view full invoice button in expanded view', async () => {
    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('202601')).toBeInTheDocument()
    })

    // Expand the invoice
    const invoiceRow = screen.getByText('202601').closest('button')
    if (invoiceRow) {
      await user.click(invoiceRow)
    }

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'students.invoices.viewFullInvoice' })).toBeInTheDocument()
    })
  })

  it('displays invoice totals correctly', async () => {
    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('202601')).toBeInTheDocument()
    })

    // Expand the invoice
    const invoiceRow = screen.getByText('202601').closest('button')
    if (invoiceRow) {
      await user.click(invoiceRow)
    }

    await waitFor(() => {
      // Check that subtotal and VAT are displayed
      expect(screen.getByText('students.invoices.subtotal')).toBeInTheDocument()
      expect(screen.getAllByText('students.invoices.vat').length).toBeGreaterThan(0)
      expect(screen.getAllByText('students.invoices.total').length).toBeGreaterThan(0)
    })
  })

  it('opens print dialog when view full invoice clicked', async () => {
    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('202601')).toBeInTheDocument()
    })

    // Expand the invoice
    const invoiceRow = screen.getByText('202601').closest('button')
    if (invoiceRow) {
      await user.click(invoiceRow)
    }

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'students.invoices.viewFullInvoice' })).toBeInTheDocument()
    })

    // Click view full invoice
    await user.click(screen.getByRole('button', { name: 'students.invoices.viewFullInvoice' }))

    await waitFor(() => {
      // Dialog should open with INVOICE header (translated)
      expect(screen.getByText('students.invoices.invoice')).toBeInTheDocument()
    })
  })

  it('shows loading state while fetching invoices', async () => {
    // Delay the mock response to see loading state
    vi.mocked(invoicesApi.getByStudent).mockImplementation(
      () => new Promise((resolve) => setTimeout(() => resolve([mockInvoiceListItem]), 100))
    )

    render(<InvoicesSection studentId={mockStudentId} />)

    // Header should be visible
    expect(screen.getByText('students.sections.invoices')).toBeInTheDocument()
  })

  it('displays balance only when greater than zero', async () => {
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      // Draft invoice has balance > 0
      expect(screen.getByText('202601')).toBeInTheDocument()
      // Paid invoice should not display balance since it's 0
      expect(screen.getByText('202602')).toBeInTheDocument()
    })
  })

  it('shows credit invoice button for sent invoices', async () => {
    const sentInvoiceListItem: InvoiceListItem = {
      ...mockPaidInvoiceListItem,
      id: 'invoice-3',
      invoiceNumber: '202603',
      status: 'Sent',
      balance: 121,
      isCreditInvoice: false,
    }
    const sentInvoice: Invoice = {
      ...mockInvoice,
      id: 'invoice-3',
      invoiceNumber: '202603',
      status: 'Sent',
      balance: 121,
      isCreditInvoice: false,
    }
    vi.mocked(invoicesApi.getByStudent).mockResolvedValue([sentInvoiceListItem])
    vi.mocked(invoicesApi.getById).mockResolvedValue(sentInvoice)

    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('202603')).toBeInTheDocument()
    })

    const invoiceRow = screen.getByText('202603').closest('button')
    if (invoiceRow) {
      await user.click(invoiceRow)
    }

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'students.creditInvoice.create' })).toBeInTheDocument()
    })
  })

  it('does not show credit invoice button for draft invoices', async () => {
    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('202601')).toBeInTheDocument()
    })

    const invoiceRow = screen.getByText('202601').closest('button')
    if (invoiceRow) {
      await user.click(invoiceRow)
    }

    await waitFor(() => {
      expect(screen.queryByRole('button', { name: 'students.creditInvoice.create' })).not.toBeInTheDocument()
    })
  })

  it('displays credit invoice badge for credit invoices', async () => {
    const creditInvoiceListItem: InvoiceListItem = {
      id: 'credit-1',
      invoiceNumber: 'C-202601',
      studentName: 'John Doe',
      description: 'Credit 202601',
      issueDate: '2026-01-20',
      dueDate: '2026-01-20',
      total: 121,
      status: 'Draft',
      balance: 0,
      isCreditInvoice: true,
      originalInvoiceId: 'invoice-1',
      originalInvoiceNumber: '202601',
    }
    vi.mocked(invoicesApi.getByStudent).mockResolvedValue([creditInvoiceListItem])

    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('C-202601')).toBeInTheDocument()
      expect(screen.getByText('students.creditInvoice.badge')).toBeInTheDocument()
    })
  })

  it('shows confirm button for draft credit invoices', async () => {
    const creditInvoiceListItem: InvoiceListItem = {
      id: 'credit-1',
      invoiceNumber: 'C-202601',
      studentName: 'John Doe',
      description: 'Credit 202601',
      issueDate: '2026-01-20',
      dueDate: '2026-01-20',
      total: 121,
      status: 'Draft',
      balance: 0,
      isCreditInvoice: true,
      originalInvoiceId: 'invoice-1',
      originalInvoiceNumber: '202601',
    }
    const creditInvoice: Invoice = {
      ...mockInvoice,
      id: 'credit-1',
      invoiceNumber: 'C-202601',
      status: 'Draft',
      isCreditInvoice: true,
      originalInvoiceId: 'invoice-1',
      originalInvoiceNumber: '202601',
    }
    vi.mocked(invoicesApi.getByStudent).mockResolvedValue([creditInvoiceListItem])
    vi.mocked(invoicesApi.getById).mockResolvedValue(creditInvoice)

    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('C-202601')).toBeInTheDocument()
    })

    const invoiceRow = screen.getByText('C-202601').closest('button')
    if (invoiceRow) {
      await user.click(invoiceRow)
    }

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'students.creditInvoice.confirm' })).toBeInTheDocument()
    })
  })

  it('collapses expanded invoice when clicked again', async () => {
    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('202601')).toBeInTheDocument()
    })

    // Expand the invoice
    const invoiceRow = screen.getByText('202601').closest('button')
    if (invoiceRow) {
      await user.click(invoiceRow)
    }

    await waitFor(() => {
      expect(screen.getByText('Piano Individual 30min - 06 Jan 2026')).toBeInTheDocument()
    })

    // Click again to collapse
    if (invoiceRow) {
      await user.click(invoiceRow)
    }

    await waitFor(() => {
      expect(screen.queryByText('Piano Individual 30min - 06 Jan 2026')).not.toBeInTheDocument()
    })
  })

  it('displays BTW number in print view when available', async () => {
    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('202601')).toBeInTheDocument()
    })

    // Expand the invoice
    const invoiceRow = screen.getByText('202601').closest('button')
    if (invoiceRow) {
      await user.click(invoiceRow)
    }

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'students.invoices.viewFullInvoice' })).toBeInTheDocument()
    })

    // Open print view
    await user.click(screen.getByRole('button', { name: 'students.invoices.viewFullInvoice' }))

    await waitFor(() => {
      expect(screen.getByText(/NL123456789B01/)).toBeInTheDocument()
    })
  })

  it('shows payment instructions for regular invoices in print view', async () => {
    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('202601')).toBeInTheDocument()
    })

    const invoiceRow = screen.getByText('202601').closest('button')
    if (invoiceRow) {
      await user.click(invoiceRow)
    }

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'students.invoices.viewFullInvoice' })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'students.invoices.viewFullInvoice' }))

    await waitFor(() => {
      expect(screen.getByText('students.invoices.paymentInstructions')).toBeInTheDocument()
      expect(screen.getByText(/NL00TEST0000000001/)).toBeInTheDocument()
    })
  })

  it('shows credit note instead of payment instructions for credit invoices in print view', async () => {
    const creditInvoice: Invoice = {
      ...mockInvoice,
      id: 'credit-1',
      invoiceNumber: 'C-202601',
      isCreditInvoice: true,
      originalInvoiceId: 'invoice-1',
      originalInvoiceNumber: '202601',
      subtotal: -100,
      vatAmount: -21,
      total: -121,
      balance: 0,
      lines: mockInvoice.lines.map((l) => ({
        ...l,
        unitPrice: -l.unitPrice,
        lineTotal: -l.lineTotal,
      })),
    }
    const creditListItem: InvoiceListItem = {
      id: 'credit-1',
      invoiceNumber: 'C-202601',
      studentName: 'John Doe',
      description: 'Credit 202601',
      issueDate: '2026-01-20',
      dueDate: '2026-01-20',
      total: -121,
      status: 'Draft',
      balance: 0,
      isCreditInvoice: true,
      originalInvoiceId: 'invoice-1',
      originalInvoiceNumber: '202601',
    }
    vi.mocked(invoicesApi.getByStudent).mockResolvedValue([creditListItem])
    vi.mocked(invoicesApi.getById).mockResolvedValue(creditInvoice)

    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('C-202601')).toBeInTheDocument()
    })

    const invoiceRow = screen.getByText('C-202601').closest('button')
    if (invoiceRow) {
      await user.click(invoiceRow)
    }

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'students.invoices.viewFullInvoice' })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'students.invoices.viewFullInvoice' }))

    await waitFor(() => {
      // Credit note should be shown
      expect(screen.getByText('students.invoices.creditNote')).toBeInTheDocument()
      expect(screen.getByText('students.invoices.creditNoteText')).toBeInTheDocument()
      // Payment instructions should NOT be shown
      expect(screen.queryByText('students.invoices.paymentInstructions')).not.toBeInTheDocument()
    })
  })

  it('shows total credit label instead of total due for credit invoices in print view', async () => {
    const creditInvoice: Invoice = {
      ...mockInvoice,
      id: 'credit-1',
      invoiceNumber: 'C-202602',
      isCreditInvoice: true,
      originalInvoiceId: 'invoice-1',
      originalInvoiceNumber: '202601',
      subtotal: -100,
      vatAmount: -21,
      total: -121,
      balance: 0,
    }
    const creditListItem: InvoiceListItem = {
      id: 'credit-1',
      invoiceNumber: 'C-202602',
      studentName: 'John Doe',
      description: 'Credit 202601',
      issueDate: '2026-01-20',
      dueDate: '2026-01-20',
      total: -121,
      status: 'Draft',
      balance: 0,
      isCreditInvoice: true,
      originalInvoiceId: 'invoice-1',
      originalInvoiceNumber: '202601',
    }
    vi.mocked(invoicesApi.getByStudent).mockResolvedValue([creditListItem])
    vi.mocked(invoicesApi.getById).mockResolvedValue(creditInvoice)

    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('C-202602')).toBeInTheDocument()
    })

    const invoiceRow = screen.getByText('C-202602').closest('button')
    if (invoiceRow) {
      await user.click(invoiceRow)
    }

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'students.invoices.viewFullInvoice' })).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'students.invoices.viewFullInvoice' }))

    await waitFor(() => {
      expect(screen.getByText('students.invoices.totalCredit')).toBeInTheDocument()
      expect(screen.queryByText('students.invoices.totalDue')).not.toBeInTheDocument()
    })
  })
})
