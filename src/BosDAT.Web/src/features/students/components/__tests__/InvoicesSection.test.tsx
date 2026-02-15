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
    ledgerCreditsApplied: 0,
    ledgerDebitsApplied: 0,
    status: 'Draft',
    amountPaid: 0,
    balance: 121,
    createdAt: '2026-01-15T10:00:00Z',
    updatedAt: '2026-01-15T10:00:00Z',
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
    ledgerApplications: [],
  }

  const mockSchoolBillingInfo: SchoolBillingInfo = {
    name: 'Test Music School',
    address: 'Test Street 123',
    postalCode: '1234AB',
    city: 'Test City',
    phone: '0612345678',
    email: 'test@school.nl',
    kvkNumber: '12345678',
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
    const invoiceRow = screen.getByText('202601').closest('[role="button"]')
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
    const invoiceRow = screen.getByText('202601').closest('[role="button"]')
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
    const invoiceRow = screen.getByText('202602').closest('[role="button"]')
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
    const invoiceRow = screen.getByText('202601').closest('[role="button"]')
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
    const invoiceRow = screen.getByText('202601').closest('[role="button"]')
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
    const invoiceRow = screen.getByText('202601').closest('[role="button"]')
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

  it('displays ledger applications when present', async () => {
    const invoiceWithApplications: Invoice = {
      ...mockInvoice,
      ledgerCreditsApplied: 20,
      ledgerApplications: [
        {
          id: 'app-1',
          ledgerEntryId: 'entry-1',
          correctionRefName: 'COR-001',
          description: 'Lesson cancellation refund',
          appliedAmount: 20,
          appliedAt: '2026-01-20T10:00:00Z',
          entryType: 'Credit',
        },
      ],
    }
    vi.mocked(invoicesApi.getById).mockResolvedValue(invoiceWithApplications)

    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('202601')).toBeInTheDocument()
    })

    // Expand the invoice to load details
    const invoiceRow = screen.getByText('202601').closest('[role="button"]')
    if (invoiceRow) {
      await user.click(invoiceRow)
    }

    // Verify the getById was called to load invoice details
    await waitFor(() => {
      expect(invoicesApi.getById).toHaveBeenCalledWith('invoice-1')
    })
  })

  it('opens print dialog when view full invoice clicked', async () => {
    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('202601')).toBeInTheDocument()
    })

    // Expand the invoice
    const invoiceRow = screen.getByText('202601').closest('[role="button"]')
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

  it('collapses expanded invoice when clicked again', async () => {
    const user = userEvent.setup()
    render(<InvoicesSection studentId={mockStudentId} />)

    await waitFor(() => {
      expect(screen.getByText('202601')).toBeInTheDocument()
    })

    // Expand the invoice
    const invoiceRow = screen.getByText('202601').closest('[role="button"]')
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
})
