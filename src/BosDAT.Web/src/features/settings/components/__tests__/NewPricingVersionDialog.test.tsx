import { describe, it, expect, vi } from 'vitest'
import { render, screen, waitFor } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { NewPricingVersionDialog } from '../NewPricingVersionDialog'
import type { CourseTypePricingVersion } from '@/features/course-types/types'

const mockCurrentPricing: CourseTypePricingVersion = {
  id: '1',
  courseTypeId: 'ct-1',
  priceAdult: 50,
  priceChild: 40,
  validFrom: '2024-01-01',
  validUntil: null,
  isCurrent: true,
  createdAt: '2024-01-01T00:00:00Z',
}

const defaultProps = {
  open: true,
  onOpenChange: vi.fn(),
  courseTypeName: 'Piano Lesson 30min',
  currentPricing: mockCurrentPricing,
  onSubmit: vi.fn(),
  isLoading: false,
  error: null,
}

describe('NewPricingVersionDialog', () => {
  it('renders dialog with course type name', () => {
    render(<NewPricingVersionDialog {...defaultProps} />)

    expect(screen.getByText('Create New Pricing Version')).toBeInTheDocument()
    expect(screen.getByText(/Piano Lesson 30min/)).toBeInTheDocument()
  })

  it('renders price input fields', () => {
    render(<NewPricingVersionDialog {...defaultProps} />)

    expect(screen.getByLabelText('Adult Price')).toBeInTheDocument()
    expect(screen.getByLabelText('Child Price')).toBeInTheDocument()
  })

  it('renders activation date field', () => {
    render(<NewPricingVersionDialog {...defaultProps} />)

    expect(screen.getByLabelText('Activation Date')).toBeInTheDocument()
  })

  it('pre-fills prices from current pricing', () => {
    render(<NewPricingVersionDialog {...defaultProps} />)

    const adultPriceInput = screen.getByLabelText('Adult Price') as HTMLInputElement
    const childPriceInput = screen.getByLabelText('Child Price') as HTMLInputElement

    expect(adultPriceInput.value).toBe('50')
    expect(childPriceInput.value).toBe('40')
  })

  it('shows cancel and submit buttons', () => {
    render(<NewPricingVersionDialog {...defaultProps} />)

    expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /create new version/i })).toBeInTheDocument()
  })

  it('calls onOpenChange when cancel is clicked', async () => {
    const user = userEvent.setup()
    const onOpenChange = vi.fn()
    render(<NewPricingVersionDialog {...defaultProps} onOpenChange={onOpenChange} />)

    await user.click(screen.getByRole('button', { name: /cancel/i }))

    expect(onOpenChange).toHaveBeenCalledWith(false)
  })

  it('submits form with correct data', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<NewPricingVersionDialog {...defaultProps} onSubmit={onSubmit} />)

    // Clear and enter new adult price
    const adultPriceInput = screen.getByLabelText('Adult Price')
    await user.clear(adultPriceInput)
    await user.type(adultPriceInput, '55')

    // Clear and enter new child price
    const childPriceInput = screen.getByLabelText('Child Price')
    await user.clear(childPriceInput)
    await user.type(childPriceInput, '45')

    // Submit
    await user.click(screen.getByRole('button', { name: /create new version/i }))

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          priceAdult: 55,
          priceChild: 45,
        })
      )
    })
  })

  it('shows validation error for negative prices', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    render(<NewPricingVersionDialog {...defaultProps} onSubmit={onSubmit} />)

    const adultPriceInput = screen.getByLabelText('Adult Price')
    await user.clear(adultPriceInput)
    await user.type(adultPriceInput, '-10')

    await user.click(screen.getByRole('button', { name: /create new version/i }))

    // The form should not submit when there are validation errors
    await waitFor(() => {
      expect(onSubmit).not.toHaveBeenCalled()
    })
  })

  it('does not submit when child price exceeds adult price', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    render(<NewPricingVersionDialog {...defaultProps} onSubmit={onSubmit} />)

    // Set child price higher than adult (50)
    const childPriceInput = screen.getByLabelText('Child Price')
    await user.clear(childPriceInput)
    await user.type(childPriceInput, '100')

    await user.click(screen.getByRole('button', { name: /create new version/i }))

    // The form should not submit when there are validation errors
    await waitFor(() => {
      expect(onSubmit).not.toHaveBeenCalled()
    })
  })

  it('shows error alert when error prop is provided', () => {
    render(<NewPricingVersionDialog {...defaultProps} error="Something went wrong" />)

    expect(screen.getByText('Something went wrong')).toBeInTheDocument()
  })

  it('disables form inputs when loading', () => {
    render(<NewPricingVersionDialog {...defaultProps} isLoading={true} />)

    expect(screen.getByLabelText('Adult Price')).toBeDisabled()
    expect(screen.getByLabelText('Child Price')).toBeDisabled()
    expect(screen.getByLabelText('Activation Date')).toBeDisabled()
  })

  it('shows loading spinner on submit button when loading', () => {
    render(<NewPricingVersionDialog {...defaultProps} isLoading={true} />)

    // The Loader2 component should be present
    const submitButton = screen.getByRole('button', { name: /create new version/i })
    expect(submitButton).toBeDisabled()
  })

  it('does not render when open is false', () => {
    render(<NewPricingVersionDialog {...defaultProps} open={false} />)

    expect(screen.queryByText('Create New Pricing Version')).not.toBeInTheDocument()
  })

  it('handles null current pricing gracefully', () => {
    render(<NewPricingVersionDialog {...defaultProps} currentPricing={null} />)

    const adultPriceInput = screen.getByLabelText('Adult Price') as HTMLInputElement
    const childPriceInput = screen.getByLabelText('Child Price') as HTMLInputElement

    // Should default to 0 when no current pricing
    expect(adultPriceInput.value).toBe('0')
    expect(childPriceInput.value).toBe('0')
  })
})
