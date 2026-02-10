import { render, screen, waitFor } from '@/test/utils'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import { InvoiceGenerationSection } from '../InvoiceGenerationSection'

vi.mock('@/features/settings/api', () => ({
  invoiceRunApi: {
    getRuns: vi.fn(),
    runBulk: vi.fn(),
  },
}))

import { invoiceRunApi } from '@/features/settings/api'

const mockRuns = [
  {
    id: '1',
    periodStart: '2026-01-01',
    periodEnd: '2026-01-31',
    periodType: 'Monthly',
    totalEnrollmentsProcessed: 10,
    totalInvoicesGenerated: 8,
    totalSkipped: 2,
    totalFailed: 0,
    totalAmount: 968.0,
    durationMs: 1500,
    status: 'Success',
    errorMessage: null,
    initiatedBy: 'admin@bosdat.nl',
    createdAt: '2026-01-15T10:00:00Z',
  },
  {
    id: '2',
    periodStart: '2026-01-01',
    periodEnd: '2026-03-31',
    periodType: 'Quarterly',
    totalEnrollmentsProcessed: 5,
    totalInvoicesGenerated: 0,
    totalSkipped: 0,
    totalFailed: 5,
    totalAmount: 0,
    durationMs: 500,
    status: 'Failed',
    errorMessage: 'Database connection error',
    initiatedBy: 'admin@bosdat.nl',
    createdAt: '2026-01-14T10:00:00Z',
  },
]

describe('InvoiceGenerationSection', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(invoiceRunApi.getRuns).mockResolvedValue({
      items: mockRuns,
      totalCount: 2,
      page: 1,
      pageSize: 5,
    })
  })

  describe('Rendering', () => {
    it('renders the section title and description', async () => {
      render(<InvoiceGenerationSection />)

      expect(screen.getByText('Invoice Generation')).toBeInTheDocument()
      expect(
        screen.getByText(/Each run is logged with statistics/)
      ).toBeInTheDocument()
    })

    it('shows loading state while fetching runs', () => {
      vi.mocked(invoiceRunApi.getRuns).mockReturnValue(new Promise(() => {}))

      render(<InvoiceGenerationSection />)

      expect(screen.getByText('Loading...')).toBeInTheDocument()
    })

    it('displays recent invoice runs', async () => {
      render(<InvoiceGenerationSection />)

      await waitFor(() => {
        expect(screen.getByText(/8 generated, 2 skipped/)).toBeInTheDocument()
      })

      expect(screen.getByText(/0 generated, 0 skipped/)).toBeInTheDocument()
    })

    it('shows empty state when no runs exist', async () => {
      vi.mocked(invoiceRunApi.getRuns).mockResolvedValue({
        items: [],
        totalCount: 0,
        page: 1,
        pageSize: 5,
      })

      render(<InvoiceGenerationSection />)

      await waitFor(() => {
        expect(screen.getByText('No invoice generation runs yet.')).toBeInTheDocument()
      })
    })

    it('displays status badges for each run', async () => {
      render(<InvoiceGenerationSection />)

      await waitFor(() => {
        expect(screen.getByText('Success')).toBeInTheDocument()
        expect(screen.getByText('Failed')).toBeInTheDocument()
      })
    })
  })

  describe('Generate Area', () => {
    it('renders period type and period selectors', async () => {
      render(<InvoiceGenerationSection />)

      await waitFor(() => {
        expect(screen.getByText('Period Type')).toBeInTheDocument()
        expect(screen.getByText('Period')).toBeInTheDocument()
      })
    })

    it('disables run button when no period is selected', async () => {
      render(<InvoiceGenerationSection />)

      await waitFor(() => {
        const button = screen.getByRole('button', { name: /Run Invoice Generation/i })
        expect(button).toBeDisabled()
      })
    })

    it('renders the generate invoices heading', async () => {
      render(<InvoiceGenerationSection />)

      await waitFor(() => {
        expect(screen.getByText('Generate Invoices')).toBeInTheDocument()
      })
    })

    it('shows description text for generating invoices', async () => {
      render(<InvoiceGenerationSection />)

      await waitFor(() => {
        expect(
          screen.getByText(/Generate invoices in bulk for all active enrollments matching/)
        ).toBeInTheDocument()
      })
    })
  })

  describe('Error State', () => {
    it('shows error state when API fails', async () => {
      vi.mocked(invoiceRunApi.getRuns).mockRejectedValue(new Error('Network error'))

      render(<InvoiceGenerationSection />)

      await waitFor(() => {
        expect(
          screen.getByText(/Failed to load invoice generation data/)
        ).toBeInTheDocument()
      })
    })
  })

  describe('Run History Display', () => {
    it('shows failed run count in details', async () => {
      render(<InvoiceGenerationSection />)

      await waitFor(() => {
        expect(screen.getByText(/5 failed/)).toBeInTheDocument()
      })
    })

    it('shows initiator information', async () => {
      render(<InvoiceGenerationSection />)

      await waitFor(() => {
        const initiatorElements = screen.getAllByText(/admin@bosdat.nl/)
        expect(initiatorElements.length).toBeGreaterThan(0)
      })
    })
  })
})
