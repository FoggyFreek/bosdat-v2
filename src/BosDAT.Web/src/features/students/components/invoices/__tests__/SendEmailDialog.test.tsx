import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { SendEmailDialog } from '../SendEmailDialog'
import { invoicesApi } from '@/features/students/api'
import type { InvoiceEmailPreview } from '@/features/students/types'

vi.mock('@/features/students/api', () => ({
  invoicesApi: {
    previewEmail: vi.fn(),
    sendEmail: vi.fn(),
  },
}))

vi.mock('@/hooks/use-toast', () => ({
  useToast: () => ({
    toast: vi.fn(),
  }),
}))

const mockPreview: InvoiceEmailPreview = {
  htmlBody: '<p>Beste Jan, bijgaand uw factuur.</p>',
  subject: 'Factuur F-2026-001 - Muziekschool',
  toEmail: 'jan@example.com',
}

describe('SendEmailDialog', () => {
  const defaultProps = {
    open: true,
    onOpenChange: vi.fn(),
    invoiceId: 'invoice-123',
    invoiceNumber: 'F-2026-001',
    studentId: 'student-456',
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders loading state while fetching preview', () => {
    vi.mocked(invoicesApi.previewEmail).mockReturnValue(new Promise(() => {}))

    render(<SendEmailDialog {...defaultProps} />)

    expect(screen.getByText('students.invoices.sendEmail.title')).toBeInTheDocument()
  })

  it('renders preview data when loaded', async () => {
    vi.mocked(invoicesApi.previewEmail).mockResolvedValue(mockPreview)

    render(<SendEmailDialog {...defaultProps} />)

    await waitFor(() => {
      expect(screen.getByText('jan@example.com')).toBeInTheDocument()
    })
    expect(screen.getByText('Factuur F-2026-001 - Muziekschool')).toBeInTheDocument()
    expect(screen.getByText('F-2026-001.pdf')).toBeInTheDocument()
  })

  it('calls sendEmail when send button is clicked', async () => {
    const user = userEvent.setup()
    vi.mocked(invoicesApi.previewEmail).mockResolvedValue(mockPreview)
    vi.mocked(invoicesApi.sendEmail).mockResolvedValue({} as never)

    render(<SendEmailDialog {...defaultProps} />)

    await waitFor(() => {
      expect(screen.getByText('jan@example.com')).toBeInTheDocument()
    })

    const sendButton = screen.getByRole('button', { name: /students\.invoices\.sendEmail\.send/i })
    await user.click(sendButton)

    await waitFor(() => {
      expect(invoicesApi.sendEmail).toHaveBeenCalledWith('invoice-123')
    })
  })

  it('calls onOpenChange when cancel button is clicked', async () => {
    const user = userEvent.setup()
    vi.mocked(invoicesApi.previewEmail).mockResolvedValue(mockPreview)

    render(<SendEmailDialog {...defaultProps} />)

    await waitFor(() => {
      expect(screen.getByText('jan@example.com')).toBeInTheDocument()
    })

    const cancelButton = screen.getByRole('button', { name: /common\.actions\.cancel/i })
    await user.click(cancelButton)

    expect(defaultProps.onOpenChange).toHaveBeenCalledWith(false)
  })

  it('does not fetch preview when dialog is closed', () => {
    render(<SendEmailDialog {...defaultProps} open={false} />)

    expect(invoicesApi.previewEmail).not.toHaveBeenCalled()
  })
})
